using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.Infrastructure.Email;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

/// <summary>
/// Reenvia o link público de aceite. Dois canais:
/// <list type="bullet">
///   <item><c>email</c> (default): envia e-mail e atualiza <c>AtualizadoEm</c> (cooldown 5 min).</item>
///   <item><c>copia</c>: não envia e-mail; só devolve o token pro front mostrar/copiar. Sem cooldown.</item>
/// </list>
///
/// Multi-tenant: termo precisa pertencer ao estab ativo.
/// </summary>
public sealed class ReenviarLinkTermoCommandHandler : ICommandHandler<ReenviarLinkTermoCommand>
{
    /// <summary>Janela de cooldown para reenvio por e-mail.</summary>
    public static readonly TimeSpan CooldownReenvio = TimeSpan.FromMinutes(5);

    private readonly ITermoEmitidoRepository _termoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IEstabelecimentoRepository _estabRepo;
    private readonly IEmailService _email;
    private readonly EmailOptions _emailOptions;
    private readonly ITermoAuditLogger _audit;
    private readonly ILogger<ReenviarLinkTermoCommandHandler> _logger;

    public ReenviarLinkTermoCommandHandler(
        ITermoEmitidoRepository termoRepo,
        IPacienteRepository pacienteRepo,
        IEstabelecimentoRepository estabRepo,
        IEmailService email,
        IOptions<EmailOptions> emailOptions,
        ITermoAuditLogger audit,
        ILogger<ReenviarLinkTermoCommandHandler> logger)
    {
        _termoRepo = termoRepo;
        _pacienteRepo = pacienteRepo;
        _estabRepo = estabRepo;
        _email = email;
        _emailOptions = emailOptions.Value;
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(ReenviarLinkTermoCommand cmd)
    {
        var termo = await _termoRepo.ObterPorIdOuNulo(cmd.TermoEmitidoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Termo não encontrado.");

        if (termo.AssinaturaTipo != AssinaturaTipo.AceiteLink)
            throw new BusinessException("Este termo não usa link de aceite.");

        if (termo.Status != StatusTermoEmitido.Pendente)
            throw new BusinessException("Termo não está pendente — não há link a reenviar.");

        if (termo.TokenExpiraEm is null || termo.TokenExpiraEm < DateTime.UtcNow)
            throw new BusinessException("Link expirado — emita um novo termo.");

        cmd.TokenAceite = termo.TokenAceite;

        var canal = string.IsNullOrWhiteSpace(cmd.Canal) ? "email" : cmd.Canal.Trim().ToLowerInvariant();
        if (canal == "copia")
        {
            // Sem envio, sem cooldown — só auditoria leve.
            await _audit.RegistrarAsync(cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId,
                "termo-link-copiado", "TermoEmitido", termo.Id);
            return;
        }

        if (canal != "email")
            throw new BusinessException("Canal inválido. Use 'email' ou 'copia'.");

        // Cooldown 5 min entre reenvios por e-mail.
        if (termo.AtualizadoEm.HasValue)
        {
            var passou = DateTime.UtcNow - termo.AtualizadoEm.Value;
            if (passou < CooldownReenvio)
            {
                var faltam = (int)Math.Ceiling((CooldownReenvio - passou).TotalMinutes);
                throw new BusinessException($"Aguarde {Math.Max(1, faltam)} min antes de reenviar o e-mail.");
            }
        }

        var paciente = await _pacienteRepo.ObterPorIdOuNulo(termo.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        if (string.IsNullOrWhiteSpace(paciente.Email))
            throw new BusinessException("Paciente sem e-mail cadastrado. Use 'copiar link'.");

        var estab = await _estabRepo.ObterPorIdOuNulo(cmd.EstabelecimentoId);
        var appUrl = (_emailOptions.AppBaseUrl ?? "https://app.imedto.com").TrimEnd('/');
        var link = $"{appUrl}/termos/aceite/{termo.TokenAceite}";

        try
        {
            await _email.EnviarAsync(
                para: paciente.Email,
                assunto: $"[{estab?.NomeFantasia ?? "Imedto"}] Termo aguardando seu aceite",
                corpoHtml: EmailTemplates.TermoAceiteParaPaciente(estab?.NomeFantasia ?? "Imedto", "Termo de consentimento", link),
                corpoTexto: $"Reenvio: acesse {link} para responder. Link válido por 30 dias.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao reenviar e-mail do link de aceite para termo {TermoId}.", termo.Id);
            throw new BusinessException("Não foi possível enviar o e-mail agora. Tente novamente em alguns instantes.");
        }

        termo.MarcarReenvioLinkEmail();
        await _termoRepo.Salvar(termo);

        await _audit.RegistrarAsync(cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId,
            "termo-link-reenviado", "TermoEmitido", termo.Id,
            metadataJson: "{\"canal\":\"email\"}");
    }
}
