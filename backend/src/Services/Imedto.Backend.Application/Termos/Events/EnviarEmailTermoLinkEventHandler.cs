using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Domain.Termos.Events;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.Infrastructure.Email;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Termos.Events;

/// <summary>
/// Reage a <see cref="TermoEmitidoEvent"/>: quando assinatura_tipo = AceiteLink e
/// canal_envio = email, dispara o e-mail com o link público para o paciente.
///
/// LGPD: e-mail vai apenas para o destinatário titular do termo (paciente.email).
/// Sem CPF, sem dados clínicos. Conteúdo limitado a: estab + título + link.
///
/// Falha de e-mail NÃO bloqueia a emissão (o termo está persistido). Logamos warning.
/// </summary>
public class EnviarEmailTermoLinkEventHandler : IEventHandler<TermoEmitidoEvent>
{
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IEstabelecimentoRepository _estabRepo;
    private readonly ITermoEmitidoRepository _termoRepo;
    private readonly IEmailService _email;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<EnviarEmailTermoLinkEventHandler> _logger;

    public EnviarEmailTermoLinkEventHandler(
        IPacienteRepository pacienteRepo,
        IEstabelecimentoRepository estabRepo,
        ITermoEmitidoRepository termoRepo,
        IEmailService email,
        IOptions<EmailOptions> emailOptions,
        ILogger<EnviarEmailTermoLinkEventHandler> logger)
    {
        _pacienteRepo = pacienteRepo;
        _estabRepo = estabRepo;
        _termoRepo = termoRepo;
        _email = email;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task Handle(TermoEmitidoEvent ev)
    {
        if (ev.AssinaturaTipo != AssinaturaTipo.AceiteLink) return;
        if (!string.Equals(ev.CanalEnvio, "email", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            var termo = await _termoRepo.ObterPorIdOuNulo(ev.TermoEmitidoId, ev.EstabelecimentoId);
            if (termo is null || string.IsNullOrEmpty(termo.TokenAceite)) return;

            var paciente = await _pacienteRepo.ObterPorIdOuNulo(ev.PacienteId, ev.EstabelecimentoId);
            if (paciente is null || string.IsNullOrWhiteSpace(paciente.Email))
            {
                _logger.LogInformation("Termo {TermoId} aceite-link emitido mas paciente sem e-mail — pulando envio.", ev.TermoEmitidoId);
                return;
            }

            var estab = await _estabRepo.ObterPorIdOuNulo(ev.EstabelecimentoId);
            var appUrl = (_emailOptions.AppBaseUrl ?? "https://app.imedto.com").TrimEnd('/');
            var link = $"{appUrl}/termos/aceite/{termo.TokenAceite}";

            await _email.EnviarAsync(
                para: paciente.Email,
                assunto: $"[{estab?.NomeFantasia ?? "Imedto"}] Termo aguardando seu aceite",
                corpoHtml: EmailTemplates.TermoAceiteParaPaciente(estab?.NomeFantasia ?? "Imedto", "Termo de consentimento", link),
                corpoTexto: $"{estab?.NomeFantasia ?? "Imedto"} emitiu um termo de consentimento. Acesse {link} para revisar e responder. Link válido por 30 dias.");
        }
        catch (Exception ex)
        {
            // Não relançar: o termo já está persistido. Emissor pode usar "Reenviar e-mail" depois.
            _logger.LogWarning(ex, "Falha ao enviar e-mail do link de aceite para termo {TermoId}.", ev.TermoEmitidoId);
        }
    }
}
