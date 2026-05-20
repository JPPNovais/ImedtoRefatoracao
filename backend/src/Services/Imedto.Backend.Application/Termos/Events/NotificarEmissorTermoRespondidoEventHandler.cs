using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Domain.Termos.Events;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.Infrastructure.Email;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Termos.Events;

/// <summary>
/// Notifica o profissional emissor por e-mail quando o paciente responde
/// (aceita ou recusa) o termo via link público.
///
/// LGPD: emissor já tem permissão para ver o paciente — o e-mail só repete
/// dados que ele já visualiza na lista de termos. CPF/dados clínicos NÃO inclusos.
///
/// O handler escuta <see cref="TermoAssinadoEvent"/> e filtra por
/// AssinaturaTipo = AceiteLink (PdfAnexado é assinatura presencial, sem notificação).
/// Recusa tem handler separado <see cref="NotificarEmissorTermoRecusadoEventHandler"/>.
/// </summary>
public class NotificarEmissorTermoAssinadoEventHandler : IEventHandler<TermoAssinadoEvent>
{
    private readonly ITermoEmitidoRepository _termoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ITermoModeloRepository _modeloRepo;
    private readonly IEmailService _email;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<NotificarEmissorTermoAssinadoEventHandler> _logger;

    public NotificarEmissorTermoAssinadoEventHandler(
        ITermoEmitidoRepository termoRepo,
        IPacienteRepository pacienteRepo,
        IUsuarioRepository usuarioRepo,
        ITermoModeloRepository modeloRepo,
        IEmailService email,
        IOptions<EmailOptions> emailOptions,
        ILogger<NotificarEmissorTermoAssinadoEventHandler> logger)
    {
        _termoRepo = termoRepo;
        _pacienteRepo = pacienteRepo;
        _usuarioRepo = usuarioRepo;
        _modeloRepo = modeloRepo;
        _email = email;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task Handle(TermoAssinadoEvent ev)
    {
        if (ev.AssinaturaTipo != AssinaturaTipo.AceiteLink) return;
        await TermoEmissorEmailDispatcher.EnviarAsync(
            ev.TermoEmitidoId, ev.EstabelecimentoId, ev.PacienteId, ev.AssinadoEm, aceito: true,
            _termoRepo, _pacienteRepo, _usuarioRepo, _modeloRepo, _email, _emailOptions, _logger);
    }
}

/// <summary>
/// Notifica o emissor quando o paciente recusa o termo via link público.
/// </summary>
public class NotificarEmissorTermoRecusadoEventHandler : IEventHandler<TermoRecusadoEvent>
{
    private readonly ITermoEmitidoRepository _termoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ITermoModeloRepository _modeloRepo;
    private readonly IEmailService _email;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<NotificarEmissorTermoRecusadoEventHandler> _logger;

    public NotificarEmissorTermoRecusadoEventHandler(
        ITermoEmitidoRepository termoRepo,
        IPacienteRepository pacienteRepo,
        IUsuarioRepository usuarioRepo,
        ITermoModeloRepository modeloRepo,
        IEmailService email,
        IOptions<EmailOptions> emailOptions,
        ILogger<NotificarEmissorTermoRecusadoEventHandler> logger)
    {
        _termoRepo = termoRepo;
        _pacienteRepo = pacienteRepo;
        _usuarioRepo = usuarioRepo;
        _modeloRepo = modeloRepo;
        _email = email;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task Handle(TermoRecusadoEvent ev)
    {
        await TermoEmissorEmailDispatcher.EnviarAsync(
            ev.TermoEmitidoId, ev.EstabelecimentoId, ev.PacienteId, ev.RecusadoEm, aceito: false,
            _termoRepo, _pacienteRepo, _usuarioRepo, _modeloRepo, _email, _emailOptions, _logger);
    }
}

/// <summary>
/// Dispatcher compartilhado pelos dois handlers (assinado/recusado). Carrega os dados
/// necessários e dispara o template. Falha de e-mail NÃO bloqueia o fluxo.
/// </summary>
internal static class TermoEmissorEmailDispatcher
{
    public static async Task EnviarAsync(
        long termoId,
        long estabelecimentoId,
        long pacienteId,
        DateTime respondidoEm,
        bool aceito,
        ITermoEmitidoRepository termoRepo,
        IPacienteRepository pacienteRepo,
        IUsuarioRepository usuarioRepo,
        ITermoModeloRepository modeloRepo,
        IEmailService email,
        EmailOptions emailOptions,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            var termo = await termoRepo.ObterPorIdOuNulo(termoId, estabelecimentoId);
            if (termo is null) return;

            var emissor = await usuarioRepo.ObterPorIdOuNulo(termo.EmitidoPorUsuarioId);
            if (emissor is null || string.IsNullOrWhiteSpace(emissor.Email)) return;

            var paciente = await pacienteRepo.ObterPorIdOuNulo(pacienteId, estabelecimentoId);
            if (paciente is null) return;

            var modelo = await modeloRepo.ObterPorIdDoEstabelecimentoOuNulo(termo.TermoModeloId, estabelecimentoId)
                ?? await modeloRepo.ObterPadraoDoSistemaPorIdOuNulo(termo.TermoModeloId);
            var tituloModelo = modelo?.Titulo ?? "Termo de consentimento";

            var appUrl = (emailOptions.AppBaseUrl ?? "https://app.imedto.com").TrimEnd('/');
            var linkDetalhe = $"{appUrl}/pacientes/{pacienteId}?aba=termos";

            var corpoHtml = EmailTemplates.TermoRespondidoParaEmissor(
                profissionalNome: emissor.NomeCompleto,
                pacienteNome: paciente.NomeCompleto ?? "Paciente",
                tituloTermo: tituloModelo,
                aceito: aceito,
                respondidoEm: respondidoEm,
                ipAssinatura: termo.IpAssinatura,
                hashIntegridade: termo.HashIntegridade,
                linkDetalheTermo: linkDetalhe);

            var assunto = aceito
                ? $"Termo assinado por {paciente.NomeCompleto}"
                : $"Termo recusado por {paciente.NomeCompleto}";

            await email.EnviarAsync(
                para: emissor.Email,
                assunto: assunto,
                corpoHtml: corpoHtml,
                corpoTexto: $"{paciente.NomeCompleto} {(aceito ? "assinou" : "recusou")} o termo \"{tituloModelo}\". Veja {linkDetalhe}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao notificar emissor sobre resposta do termo {TermoId}.", termoId);
        }
    }
}
