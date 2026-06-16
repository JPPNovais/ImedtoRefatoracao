using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging;

namespace Imedto.Backend.Application.Admin.Migracao;

/// <summary>
/// Desfaz a migração de um job concluído: reverte SOMENTE os registros com
/// status <c>importado_criado</c> (R9/CA17/D12).
///
/// Registros com status <c>importado_atualizado</c> (já existiam antes da migração)
/// NÃO são tocados — o relatório avisa quantos foram mantidos.
///
/// Se um registro criado pelo job já foi referenciado por outro fluxo (FK ativa),
/// a remoção é pulada com segurança: o registro é reportado em <c>TotalNaoRevertidos</c>.
///
/// LGPD: sem PII em logs. Audit: status do job muda para "desfeito" (CA20).
/// Multi-tenant: JobId é resolvido sem filtro de tenant porque o caller já é admin.
/// </summary>
public sealed class DesfazerMigracaoCommandHandler
{
    private readonly IMigracaoJobRepository _jobRepo;
    private readonly IMigracaoRegistroRepository _registroRepo;
    private readonly IMigracaoJobEventoRepository _eventoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IItemInventarioRepository _itemRepo;
    private readonly ICategoriaEstoqueRepository _categoriaRepo;
    private readonly IFabricanteEstoqueRepository _fabricanteRepo;
    private readonly IFornecedorEstoqueRepository _fornecedorRepo;
    private readonly ILocalEstoqueRepository _localRepo;
    private readonly ICatalogoCirurgiaRepository _cirurgiaRepo;
    private readonly ICatalogoProdutoRepository _produtoRepo;
    private readonly ILogger<DesfazerMigracaoCommandHandler> _logger;

    public DesfazerMigracaoCommandHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoRegistroRepository registroRepo,
        IMigracaoJobEventoRepository eventoRepo,
        IPacienteRepository pacienteRepo,
        IAgendamentoRepository agendamentoRepo,
        IItemInventarioRepository itemRepo,
        ICategoriaEstoqueRepository categoriaRepo,
        IFabricanteEstoqueRepository fabricanteRepo,
        IFornecedorEstoqueRepository fornecedorRepo,
        ILocalEstoqueRepository localRepo,
        ICatalogoCirurgiaRepository cirurgiaRepo,
        ICatalogoProdutoRepository produtoRepo,
        ILogger<DesfazerMigracaoCommandHandler> logger)
    {
        _jobRepo       = jobRepo;
        _registroRepo  = registroRepo;
        _eventoRepo    = eventoRepo;
        _pacienteRepo  = pacienteRepo;
        _agendamentoRepo = agendamentoRepo;
        _itemRepo      = itemRepo;
        _categoriaRepo = categoriaRepo;
        _fabricanteRepo = fabricanteRepo;
        _fornecedorRepo = fornecedorRepo;
        _localRepo     = localRepo;
        _cirurgiaRepo  = cirurgiaRepo;
        _produtoRepo   = produtoRepo;
        _logger        = logger;
    }

    public async Task<RelatorioDesfazimentoResult> Handle(long jobId, CancellationToken ct = default)
    {
        if (jobId <= 0) throw new BusinessException("Job inválido.");

        var job = await _jobRepo.ObterPorIdAdminOuNulo(jobId, ct)
            ?? throw new BusinessException("Job não encontrado.");

        // Domínio valida que só jobs concluídos podem ser desfeitos
        var statusAnterior = job.Status;
        job.MarcarDesfeito();

        // Contagem de atualizados (não revertidos — CA17: aviso obrigatório)
        var todos = await _registroRepo.ListarPorJob(jobId, ct);
        var totalAtualizados = todos.Count(r => r.Status == "importado_atualizado");

        // Somente os criados por este job são revertidos
        var criados = await _registroRepo.ListarCriadosPorJob(jobId, ct);

        var revertidos = 0;
        var naoRevertidos = 0;

        // Ordem reversa à ordem de carga (FK-safe): primeiro os que dependem dos outros,
        // depois os base. Agendamentos e itens antes de pacientes e cadastros.
        var ordenados = OrdenarParaRollback(criados);

        foreach (var reg in ordenados)
        {
            if (reg.EntidadeAlvoId is null)
            {
                // Não tem ID alvo registrado — nada a reverter (proteção)
                naoRevertidos++;
                continue;
            }

            var reverteu = await TentarReverterAsync(job.EstabelecimentoId, reg, ct);
            if (reverteu)
            {
                reg.MarcarPulado("revertido pelo desfazer");
                revertidos++;
            }
            else
            {
                naoRevertidos++;
                _logger.LogWarning(
                    "[Desfazer:{JobId}] Registro {RegistroId} entidade={Entidade} não pôde ser revertido (FK ativa ou não encontrado).",
                    jobId, reg.Id, reg.Entidade);
            }

            await _registroRepo.Salvar(reg, ct);
        }

        await _jobRepo.Salvar(job, ct);

        var evt = MigracaoJobEvento.Criar(job.Id, job.EstabelecimentoId, statusAnterior, job.Status, usuarioId: null);
        await _eventoRepo.Gravar(evt, ct);

        // Aviso obrigatório de CA17
        var aviso = totalAtualizados > 0
            ? $"{revertidos} registros criados revertidos. {totalAtualizados} registros atualizados mantidos (não revertidos — pré-existiam antes da migração)."
            : $"{revertidos} registros criados revertidos.";

        if (naoRevertidos > 0)
            aviso += $" {naoRevertidos} criados não puderam ser revertidos (referenciados por outro fluxo).";

        return new RelatorioDesfazimentoResult
        {
            TotalRevertidos          = revertidos,
            TotalNaoRevertidos       = naoRevertidos,
            TotalAtualizadosMantidos = totalAtualizados,
            Aviso                    = aviso,
        };
    }

    /// <summary>
    /// Ordena os registros para reversão segura: entidades que referenciam outras primeiro
    /// (agendamento antes de paciente; item_estoque antes de categoria/fabricante/fornecedor/local).
    /// Inverso da ordem de carga do CarregarOnda1JobHandler.
    /// </summary>
    private static List<MigracaoRegistro> OrdenarParaRollback(List<MigracaoRegistro> registros)
    {
        // Ordem inversa à de carga: os que dependem de outros entram primeiro no rollback
        string[] ordem =
        [
            "agendamento",
            "paciente",
            "item_estoque",
            "produto_orcamento",
            "procedimento_orcamento",
            "local_estoque",
            "fabricante_estoque",
            "fornecedor_estoque",
            "categoria_estoque",
        ];

        return registros
            .OrderBy(r =>
            {
                var idx = Array.IndexOf(ordem, r.Entidade);
                return idx >= 0 ? idx : 999;
            })
            .ToList();
    }

    /// <summary>
    /// Tenta remover a entidade de domínio correspondente ao registro.
    /// Retorna <c>true</c> se reverteu, <c>false</c> se não encontrou ou se FK impede.
    /// Nunca lança — integridade prevalece sobre reversão forçada.
    /// </summary>
    private async Task<bool> TentarReverterAsync(long estabelecimentoId, MigracaoRegistro reg, CancellationToken ct)
    {
        var id = reg.EntidadeAlvoId!.Value;

        try
        {
            return reg.Entidade switch
            {
                "paciente"               => await ReverterPacienteAsync(id, estabelecimentoId),
                "agendamento"            => await ReverterAgendamentoAsync(id, estabelecimentoId),
                "item_estoque"           => await ReverterItemInventarioAsync(id, estabelecimentoId),
                "categoria_estoque"      => await ReverterCategoriaAsync(id, estabelecimentoId),
                "fabricante_estoque"     => await ReverterFabricanteAsync(id, estabelecimentoId),
                "fornecedor_estoque"     => await ReverterFornecedorAsync(id, estabelecimentoId),
                "local_estoque"          => await ReverterLocalAsync(id, estabelecimentoId),
                "procedimento_orcamento" => await ReverterCirurgiaAsync(id, estabelecimentoId),
                "produto_orcamento"      => await ReverterProdutoAsync(id, estabelecimentoId),
                _                        => false, // entidade desconhecida — não reverter
            };
        }
        catch (Exception)
        {
            // FK constraint ou outro erro de integridade → reportar como não-revertido
            // (não propagar — uma FK ativa em outro fluxo é esperada e segura)
            return false;
        }
    }

    private async Task<bool> ReverterPacienteAsync(long id, long estabelecimentoId)
    {
        var entidade = await _pacienteRepo.ObterPorIdOuNulo(id, estabelecimentoId);
        if (entidade is null) return false;
        await _pacienteRepo.Remover(entidade);
        return true;
    }

    private async Task<bool> ReverterAgendamentoAsync(long id, long estabelecimentoId)
    {
        var entidade = await _agendamentoRepo.ObterPorIdOuNulo(id, estabelecimentoId);
        if (entidade is null) return false;
        await _agendamentoRepo.Remover(entidade);
        return true;
    }

    private async Task<bool> ReverterItemInventarioAsync(long id, long estabelecimentoId)
    {
        var entidade = await _itemRepo.ObterPorIdOuNulo(id, estabelecimentoId);
        if (entidade is null) return false;
        await _itemRepo.Remover(entidade);
        return true;
    }

    private async Task<bool> ReverterCategoriaAsync(long id, long estabelecimentoId)
    {
        var entidade = await _categoriaRepo.ObterPorIdOuNulo(id, estabelecimentoId);
        if (entidade is null) return false;
        await _categoriaRepo.Remover(entidade);
        return true;
    }

    private async Task<bool> ReverterFabricanteAsync(long id, long estabelecimentoId)
    {
        var entidade = await _fabricanteRepo.ObterPorIdOuNulo(id, estabelecimentoId);
        if (entidade is null) return false;
        await _fabricanteRepo.Remover(entidade);
        return true;
    }

    private async Task<bool> ReverterFornecedorAsync(long id, long estabelecimentoId)
    {
        var entidade = await _fornecedorRepo.ObterPorIdOuNulo(id, estabelecimentoId);
        if (entidade is null) return false;
        await _fornecedorRepo.Remover(entidade);
        return true;
    }

    private async Task<bool> ReverterLocalAsync(long id, long estabelecimentoId)
    {
        var entidade = await _localRepo.ObterPorIdOuNulo(id, estabelecimentoId);
        if (entidade is null) return false;
        await _localRepo.Remover(entidade);
        return true;
    }

    private async Task<bool> ReverterCirurgiaAsync(long id, long estabelecimentoId)
    {
        var entidade = await _cirurgiaRepo.ObterPorIdOuNulo(id, estabelecimentoId);
        if (entidade is null) return false;
        await _cirurgiaRepo.Remover(entidade);
        return true;
    }

    private async Task<bool> ReverterProdutoAsync(long id, long estabelecimentoId)
    {
        var entidade = await _produtoRepo.ObterPorIdOuNulo(id, estabelecimentoId);
        if (entidade is null) return false;
        await _produtoRepo.Remover(entidade);
        return true;
    }
}
