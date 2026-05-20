using System.Globalization;
using System.Text;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

/// <summary>
/// Registra aceite ou recusa do paciente via link público (Fase 4).
///
/// Fluxo:
/// 1. Carrega termo por token (sem filtro de tenant — token é segredo).
/// 2. Token inexistente / expirado / termo não-pendente: a controller responde 410.
///    Aqui levantamos BusinessException com mensagem genérica.
/// 3. Idempotência: termo já assinado/recusado → não erro, devolve <see cref="ResultadoRespostaPublica.JaRespondido"/>.
/// 4. Hash de integridade: recalcular SHA-256 do snapshot e comparar — defesa contra
///    corrupção/fraude. Divergência = 500 (não é input do usuário).
/// 5. (Opcional) nomeConfirmado: comparar com paciente.NomeCompleto (case/acento-insensitivo).
/// 6. Marca aceite ou recusa no aggregate (dispara TermoAssinadoEvent / TermoRecusadoEvent).
/// 7. Salva log de acesso "aceitou" ou "recusou".
/// </summary>
public sealed class RegistrarRespostaPublicaTermoCommandHandler : ICommandHandler<RegistrarRespostaPublicaTermoCommand>
{
    /// <summary>Mensagem genérica devolvida em todos os erros do fluxo público.</summary>
    public const string MensagemLinkInvalido = "Este link expirou ou já foi respondido. Entre em contato com o estabelecimento.";

    private readonly ITermoEmitidoRepository _termoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IEventBus _eventBus;

    public RegistrarRespostaPublicaTermoCommandHandler(
        ITermoEmitidoRepository termoRepo,
        IPacienteRepository pacienteRepo,
        IEventBus eventBus)
    {
        _termoRepo = termoRepo;
        _pacienteRepo = pacienteRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(RegistrarRespostaPublicaTermoCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.TokenAceite))
            throw new BusinessException(MensagemLinkInvalido);

        var termo = await _termoRepo.ObterPorTokenOuNulo(cmd.TokenAceite);
        if (termo is null)
            throw new BusinessException(MensagemLinkInvalido);

        // Idempotência: termo já respondido/revogado/expirado retorna 200 sem mudar nada.
        if (termo.Status != StatusTermoEmitido.Pendente)
        {
            cmd.Resultado = ResultadoRespostaPublica.JaRespondido;
            await SalvarLog(termo.Id, cmd, acao: "tentativa_idempotente");
            return;
        }

        // Token expirado: o controller traduz BusinessException em 410.
        if (termo.TokenExpiraEm is null || termo.TokenExpiraEm < DateTime.UtcNow)
        {
            await SalvarLog(termo.Id, cmd, acao: "tentativa_expirada");
            throw new BusinessException(MensagemLinkInvalido);
        }

        // Hash de integridade: defense-in-depth contra corrupção do snapshot no banco.
        var hashRecalculado = TermoEmitido.CalcularHashSha256(termo.ConteudoSnapshotHtml);
        if (!string.Equals(hashRecalculado, termo.HashIntegridade, StringComparison.Ordinal))
        {
            // Erro técnico — não expor pro paciente. O controller mapeia para genérico.
            throw new InvalidOperationException(
                $"Hash de integridade divergente para termo {termo.Id}. Possível corrupção do snapshot.");
        }

        // Validação opcional de nome (case/acento-insensitivo).
        if (!string.IsNullOrWhiteSpace(cmd.NomeConfirmado))
        {
            var paciente = await _pacienteRepo.ObterPorIdOuNulo(termo.PacienteId, termo.EstabelecimentoId);
            if (paciente is null)
                throw new BusinessException(MensagemLinkInvalido);

            if (!NomesEquivalentes(paciente.NomeCompleto, cmd.NomeConfirmado))
                throw new BusinessException("Confirme seu nome completo conforme cadastrado.");
        }

        if (cmd.Aceito)
            termo.RegistrarAceitePublico(cmd.IpOrigem, cmd.UserAgent);
        else
            termo.RegistrarRecusaPublica(cmd.IpOrigem, cmd.UserAgent);

        await _termoRepo.Salvar(termo);
        await SalvarLog(termo.Id, cmd, acao: cmd.Aceito ? "aceitou" : "recusou");

        foreach (var ev in termo.DomainEvents)
            await _eventBus.Publish(ev);
        termo.ClearDomainEvents();

        cmd.Resultado = ResultadoRespostaPublica.RespondidoAgora;
    }

    private Task SalvarLog(long termoId, RegistrarRespostaPublicaTermoCommand cmd, string acao)
        => _termoRepo.SalvarAcessoLog(TermoEmitidoAcessoLog.Registrar(termoId, cmd.IpOrigem, cmd.UserAgent, acao));

    /// <summary>
    /// Compara dois nomes ignorando case, acentos e múltiplos espaços. Não exige
    /// igualdade exata (o paciente pode digitar com ou sem acento).
    /// </summary>
    internal static bool NomesEquivalentes(string nomeCadastrado, string nomeDigitado)
    {
        var a = Normalizar(nomeCadastrado);
        var b = Normalizar(nomeDigitado);
        if (a.Length == 0 || b.Length == 0) return false;
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalizar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return string.Empty;
        var trim = nome.Trim();
        // Colapsa múltiplos espaços em um.
        var partes = trim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var compactado = string.Join(' ', partes);
        // Remove acentos.
        var decomposto = compactado.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
