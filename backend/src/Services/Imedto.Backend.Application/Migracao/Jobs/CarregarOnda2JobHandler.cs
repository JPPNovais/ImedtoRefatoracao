using System.Text.Json;
using Imedto.Backend.Application.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging;

namespace Imedto.Backend.Application.Migracao.Jobs;

/// <summary>
/// Job de carga da Onda 2 — prontuário histórico (briefing 2026-06-15_001 § Marco 5).
///
/// Espelha <see cref="CarregarOnda1JobHandler"/> na estrutura; diferenças:
///
/// <list type="bullet">
///   <item>CA13 — bloqueia se a Onda 1 (pacientes) do mesmo tenant ainda não concluiu.</item>
///   <item>CA14 — vincula o prontuário ao paciente via CPF ou documento internacional.
///     Sem identificador válido → registro rejeitado ("paciente não identificado").
///     Nunca cria paciente a partir do prontuário.</item>
///   <item>CA15 — origem com campos identificáveis → evolução estruturada;
///     origem sem estrutura → anexo histórico pesquisável no paciente.</item>
///   <item>CA16 — receita controlada com combinação tipo+notificação inválida → rejeitada
///     (BusinessException do command).</item>
///   <item>CA21 — audit LGPD: cada escrita em prontuário gera linha via
///     <see cref="IProntuarioAcessoLogService"/>.</item>
/// </list>
/// </summary>
public sealed class CarregarOnda2JobHandler : IJobHandler
{
    public string Nome => "carregar-onda2-migracao";

    private const int TamanhoLote = 100;

    // Guid fixo de sistema — representa o "usuário-migração" nos audit logs de prontuário.
    // Não é um usuário real; serve para separar linhas de audit da carga de migração
    // das evoluções clínicas normais. Sem PII.
    internal static readonly Guid AutorSistemaId = new("00000000-0000-0000-0000-000000000001");

    private readonly IMigracaoJobRepository _jobRepo;
    private readonly IMigracaoRegistroRepository _registroRepo;
    private readonly IMigracaoPacienteLookup _pacienteLookup;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IniciarProntuarioCommandHandler _iniciarProntuarioHandler;
    private readonly RegistrarEvolucaoCommandHandler _registrarEvolucaoHandler;
    private readonly AdicionarAnexoCommandHandler _adicionarAnexoHandler;
    private readonly ILogger<CarregarOnda2JobHandler> _logger;

    public CarregarOnda2JobHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoRegistroRepository registroRepo,
        IMigracaoPacienteLookup pacienteLookup,
        IProntuarioRepository prontuarioRepo,
        IniciarProntuarioCommandHandler iniciarProntuarioHandler,
        RegistrarEvolucaoCommandHandler registrarEvolucaoHandler,
        AdicionarAnexoCommandHandler adicionarAnexoHandler,
        ILogger<CarregarOnda2JobHandler> logger)
    {
        _jobRepo = jobRepo;
        _registroRepo = registroRepo;
        _pacienteLookup = pacienteLookup;
        _prontuarioRepo = prontuarioRepo;
        _iniciarProntuarioHandler = iniciarProntuarioHandler;
        _registrarEvolucaoHandler = registrarEvolucaoHandler;
        _adicionarAnexoHandler = adicionarAnexoHandler;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var job = await _jobRepo.ObterMaisAntigoMigrandoOnda2OuNulo(ct);
        if (job is null) return;

        _logger.LogInformation("[Job:{Nome}] Iniciando carga Onda 2 para job {JobId}.", Nome, job.Id);

        try
        {
            await ProcessarJobAsync(job, ct);
        }
        catch (Exception ex)
        {
            // Addendum 002 — R-B2/CA26: marca falhou em vez de re-lançar (que travava o job mudo).
            // CA27 — a espera legítima da Onda 1 usa "return" explícito ANTES deste try/catch
            // (ProcessarJobAsync retorna sem lançar quando a Onda 1 ainda não concluiu).
            _logger.LogError(ex, "[Job:{Nome}] Falha inesperada no job {JobId}.", Nome, job.Id);
            try
            {
                job.MarcarFalhou("falha inesperada na carga");
                await _jobRepo.Salvar(job, ct);
            }
            catch (Exception salvarEx)
            {
                _logger.LogError(salvarEx,
                    "[Job:{Nome}] Falha ao persistir status 'falhou' do job {JobId}.", Nome, job.Id);
            }
        }
    }

    private async Task ProcessarJobAsync(MigracaoJob job, CancellationToken ct)
    {
        // CA13 — Onda 1 precisa estar concluída para este tenant antes de processar Onda 2.
        // Verifica se existe job de Onda 1 ainda "migrando" ou "aguardando" para o mesmo tenant.
        var onda1Bloqueante = await _jobRepo.ExisteOnda1AtivaParaTenant(job.EstabelecimentoId, ct);
        if (onda1Bloqueante)
        {
            _logger.LogInformation(
                "[Job:{Nome}] Job {JobId} bloqueado — Onda 1 do tenant {Tenant} ainda não concluiu (CA13).",
                Nome, job.Id, job.EstabelecimentoId);
            // Não altera o status — job será reprocessado na próxima rodada do scheduler.
            return;
        }

        var todos = await _registroRepo.ListarPorJob(job.Id, ct);
        var pendentes = todos.Where(r => r.Status == "pendente").ToList();

        if (pendentes.Count == 0)
        {
            job.MarcarConcluido();
            await _jobRepo.Salvar(job, ct);
            return;
        }

        var temRejeitados = false;

        for (var i = 0; i < pendentes.Count; i += TamanhoLote)
        {
            if (ct.IsCancellationRequested) break;
            var lote = pendentes.Skip(i).Take(TamanhoLote).ToList();
            foreach (var reg in lote)
            {
                await ProcessarRegistroAsync(job.EstabelecimentoId, reg, ct);
                if (reg.Status == "rejeitado") temRejeitados = true;
                await _registroRepo.Salvar(reg, ct);
            }
        }

        if (temRejeitados)
            job.MarcarConcluidoComErros();
        else
            job.MarcarConcluido();

        await _jobRepo.Salvar(job, ct);

        _logger.LogInformation("[Job:{Nome}] Job {JobId} concluído. Status: {Status}.", Nome, job.Id, job.Status);
    }

    private async Task ProcessarRegistroAsync(long estabelecimentoId, MigracaoRegistro reg, CancellationToken ct)
    {
        try
        {
            var payload = ParsePayload(reg.PayloadBruto);

            switch (reg.Entidade)
            {
                case "prontuario_evolucao":
                    await ProcessarEvolucaoAsync(estabelecimentoId, reg, payload, ct);
                    break;
                case "prontuario_anexo":
                    await ProcessarAnexoAsync(estabelecimentoId, reg, payload, ct);
                    break;
                default:
                    reg.MarcarPulado("entidade não suportada nesta onda");
                    break;
            }
        }
        catch (BusinessException ex)
        {
            reg.MarcarRejeitado(ex.Message);
        }
        // Exceção técnica sobe — não capturar aqui (sobe para o catch do ProcessarJobAsync).
    }

    /// <summary>
    /// Processa uma evolução estruturada (CA14, CA15, CA21).
    ///
    /// Origem com campos identificáveis (conteudo_json preenchido) → evolução no prontuário.
    /// Origem sem estrutura (conteudo_json ausente ou vazio) → cai em anexo histórico (CA15).
    /// </summary>
    private async Task ProcessarEvolucaoAsync(
        long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        // CA14 — resolve vínculo por CPF → documento internacional
        var info = await ResolverPacienteOuNulo(payload, estId, ct);
        if (info is null)
        {
            reg.MarcarRejeitado("paciente não identificado");
            return;
        }

        // CA15 — honestidade estrutural: sem conteudo_json → não fabrica evolução
        var conteudoJson = G(payload, "conteudo_json");
        if (string.IsNullOrWhiteSpace(conteudoJson))
        {
            // Sem estrutura: importa como anexo histórico em texto plano (CA15)
            await ImportarComoAnexoHistoricoAsync(estId, reg, payload, info, ct);
            return;
        }

        // Garante que o prontuário existe; se não, inicia com modelo padrão do tenant.
        await GarantirProntuarioAsync(info, estId, ct);

        if (!long.TryParse(G(payload, "modelo_prontuario_id"), out var modeloId) || modeloId <= 0)
            modeloId = 0; // handler usa o do prontuário quando 0

        // CA16 — receita controlada: o command de evolução (RegistrarEvolucaoCommand) executa as
        // validações do domínio (BusinessException se combinação tipo+notificação inválida).
        var cmd = new RegistrarEvolucaoCommand
        {
            PacienteId = info.PacienteId,
            EstabelecimentoId = estId,
            AutorUsuarioId = AutorSistemaId,
            ConteudoJson = conteudoJson,
            ModeloDeProntuarioId = modeloId > 0 ? modeloId : null,
        };

        // CA21 — audit é gerado dentro do RegistrarEvolucaoCommandHandler via IProntuarioAcessoLogService.
        await _registrarEvolucaoHandler.Handle(cmd);

        reg.MarcarImportadoCriado(cmd.EvolucaoIdCriada);
    }

    /// <summary>
    /// Processa um prontuário sem estrutura: grava como anexo/documento histórico pesquisável (CA15).
    /// Usa AdicionarAnexoCommand com mime "text/plain" e conteúdo do campo "texto_livre".
    ///
    /// Também é chamado quando prontuario_evolucao não tem conteudo_json preenchido.
    /// </summary>
    private async Task ProcessarAnexoAsync(
        long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        var info = await ResolverPacienteOuNulo(payload, estId, ct);
        if (info is null)
        {
            reg.MarcarRejeitado("paciente não identificado");
            return;
        }

        await ImportarComoAnexoHistoricoAsync(estId, reg, payload, info, ct);
    }

    /// <summary>
    /// Importa texto livre ou PDF histórico como anexo pesquisável (CA15).
    /// Não cria evolução estruturada inventada.
    /// CA21 — audit via AdicionarAnexoCommandHandler.
    /// </summary>
    private async Task ImportarComoAnexoHistoricoAsync(
        long estId, MigracaoRegistro reg, Dictionary<string, string> payload,
        PacienteMigracaoInfo info, CancellationToken ct)
    {
        // Garante que o prontuário existe
        await GarantirProntuarioAsync(info, estId, ct);

        var textoLivre = G(payload, "texto_livre") ?? G(payload, "conteudo") ?? "";
        var nomeArquivo = G(payload, "nome_arquivo") ?? "historico_migrado.txt";
        var dataRegistro = G(payload, "data_registro") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Conteúdo como stream de bytes UTF-8
        var conteudoBytes = System.Text.Encoding.UTF8.GetBytes(
            $"[Histórico importado em {dataRegistro}]\n\n{textoLivre}");
        using var stream = new MemoryStream(conteudoBytes);

        var cmd = new AdicionarAnexoCommand
        {
            PacienteId = info.PacienteId,
            EstabelecimentoId = estId,
            AutorUsuarioId = AutorSistemaId,
            NomeOriginal = nomeArquivo,
            MimeType = "text/plain",
            TamanhoBytes = conteudoBytes.Length,
            Conteudo = stream,
        };

        // CA21 — audit é gerado dentro do AdicionarAnexoCommandHandler.
        await _adicionarAnexoHandler.Handle(cmd);

        reg.MarcarImportadoCriado(cmd.AnexoIdCriado);
    }

    /// <summary>
    /// Garante que o paciente tem prontuário; inicia se necessário.
    /// Retorna o prontuário id.
    /// </summary>
    private async Task<long> GarantirProntuarioAsync(
        PacienteMigracaoInfo info, long estId, CancellationToken ct)
    {
        if (info.ProntuarioId.HasValue)
            return info.ProntuarioId.Value;

        // Precisa iniciar prontuário — busca modelo padrão do tenant
        var modeloId = await _pacienteLookup.ObterIdModeloPadraoProntuarioOuNulo(estId, ct)
            ?? throw new BusinessException("Nenhum modelo de prontuário ativo encontrado para o estabelecimento.");

        var cmd = new IniciarProntuarioCommand
        {
            PacienteId = info.PacienteId,
            EstabelecimentoId = estId,
            ModeloDeProntuarioId = modeloId,
            SolicitanteUsuarioId = AutorSistemaId,
        };

        await _iniciarProntuarioHandler.Handle(cmd);

        // Recarrega para obter o id persistido
        var prontuario = await _prontuarioRepo.ObterPorPaciente(info.PacienteId, estId)
            ?? throw new BusinessException("Falha ao iniciar prontuário durante migração.");

        return prontuario.Id;
    }

    /// <summary>
    /// CA14 — resolve vínculo por CPF (prioritário) → documento internacional.
    /// Retorna null se o identificador não resolver (registro deve ser rejeitado pelo caller).
    /// Nunca cria paciente.
    /// </summary>
    private async Task<PacienteMigracaoInfo?> ResolverPacienteOuNulo(
        Dictionary<string, string> payload, long estId, CancellationToken ct)
    {
        var cpf = G(payload, "cpf");
        if (cpf != null)
            return await _pacienteLookup.ObterPorCpfOuNulo(cpf, estId, ct);

        var docInt = G(payload, "documento_internacional");
        if (docInt != null)
            return await _pacienteLookup.ObterPorDocumentoInternacionalOuNulo(docInt, estId, ct);

        return null; // sem identificador → caller rejeita com "paciente não identificado"
    }

    private static Dictionary<string, string> ParsePayload(string payloadBruto)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(payloadBruto)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static string? G(Dictionary<string, string> p, string key)
        => p.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
}
