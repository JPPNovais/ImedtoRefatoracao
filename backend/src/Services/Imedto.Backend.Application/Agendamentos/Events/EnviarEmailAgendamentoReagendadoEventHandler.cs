using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Agendamentos.Events;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Infrastructure.Auth;
using Imedto.Backend.Infrastructure.Email;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Time;

namespace Imedto.Backend.Application.Agendamentos.Events;

/// <summary>
/// Reage a <see cref="AgendamentoReagendadoEvent"/>: gera token de confirmação pública (Fase 2),
/// persiste, e envia e-mail ao paciente com o link de confirmação.
///
/// Decisão de implementação (Fase 2): o token é gerado aqui (event handler), antes de montar
/// o e-mail. O agendamento é carregado, o token gerado via GerarTokenConfirmacao() e persistido
/// via Salvar() antes do envio. Assim o link é válido ao chegar na caixa postal do paciente.
///
/// LGPD:
///   * Sem PII no log (sem destinatário, assunto ou corpo).
///   * E-mail sem CPF ou dados clínicos — apenas: estab, profissional, tipo, data/hora e link.
///   * Sem e-mail do paciente → pula com LogInformation (sem PII); não falha.
///   * Falha no envio → LogWarning (sem PII); não relança (reagendamento já persistido).
///   * Token NÃO logado em texto claro.
/// </summary>
public class EnviarEmailAgendamentoReagendadoEventHandler : IEventHandler<AgendamentoReagendadoEvent>
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IEstabelecimentoRepository _estabRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmailService _email;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<EnviarEmailAgendamentoReagendadoEventHandler> _logger;

    public EnviarEmailAgendamentoReagendadoEventHandler(
        IAgendamentoRepository agendamentoRepo,
        IPacienteRepository pacienteRepo,
        IEstabelecimentoRepository estabRepo,
        IUsuarioRepository usuarioRepo,
        IEmailService email,
        IOptions<EmailOptions> emailOptions,
        ILogger<EnviarEmailAgendamentoReagendadoEventHandler> logger)
    {
        _agendamentoRepo = agendamentoRepo;
        _pacienteRepo = pacienteRepo;
        _estabRepo = estabRepo;
        _usuarioRepo = usuarioRepo;
        _email = email;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task Handle(AgendamentoReagendadoEvent ev)
    {
        try
        {
            // R8: paciente sem e-mail → pular silenciosamente, sem PII.
            var paciente = await _pacienteRepo.ObterPorIdOuNulo(ev.PacienteId, ev.EstabelecimentoId);
            if (paciente is null || string.IsNullOrWhiteSpace(paciente.Email))
            {
                _logger.LogInformation(
                    "Agendamento {AgendamentoId} remarcado mas paciente sem e-mail — pulando envio.",
                    ev.AgendamentoId);
                return;
            }

            var estab = await _estabRepo.ObterPorIdOuNulo(ev.EstabelecimentoId);
            var nomeEstab = estab?.NomeFantasia ?? "Imedto";

            var profissional = await _usuarioRepo.ObterPorIdOuNulo(ev.ProfissionalUsuarioId);
            var nomeProfissional = profissional?.NomeCompleto ?? "—";

            // Fase 2 — R17: gerar token de confirmação pública e persistir antes de enviar o e-mail.
            var agendamento = await _agendamentoRepo.ObterPorIdOuNulo(ev.AgendamentoId, ev.EstabelecimentoId);
            var tipoServico = agendamento?.TipoServico ?? "Consulta";

            string? linkConfirmacao = null;
            if (agendamento is not null)
            {
                agendamento.GerarTokenConfirmacao();
                await _agendamentoRepo.Salvar(agendamento);

                var appUrl = (_emailOptions.AppBaseUrl ?? "https://app.imedto.com").TrimEnd('/');
                // R20 / LGPD: token não logado em texto claro — só embutido no link do e-mail.
                linkConfirmacao = $"{appUrl}/agendamentos/confirmar/{agendamento.TokenConfirmacao}";
            }

            var novoInicioEmBrasilia = ev.NovoInicioPrevisto.ToBrasilia();

            if (linkConfirmacao is not null)
            {
                // Fase 2: e-mail com link de confirmação.
                await _email.EnviarAsync(
                    para: paciente.Email,
                    assunto: $"[{nomeEstab}] Seu agendamento foi remarcado — confirme sua presença",
                    corpoHtml: EmailTemplates.AgendamentoConfirmacaoLinkParaPaciente(
                        nomeEstab,
                        tipoServico,
                        nomeProfissional,
                        novoInicioEmBrasilia,
                        linkConfirmacao),
                    corpoTexto: $"{nomeEstab} remarcou seu agendamento de {tipoServico} com {nomeProfissional} para {novoInicioEmBrasilia:dd/MM/yyyy 'às' HH:mm}. Confirme sua presença pelo link: {linkConfirmacao}");
            }
            else
            {
                // Degradação graciosa: sem agendamento no evento, usa template Fase 1 sem link.
                await _email.EnviarAsync(
                    para: paciente.Email,
                    assunto: $"[{nomeEstab}] Seu agendamento foi remarcado",
                    corpoHtml: EmailTemplates.AgendamentoRemarcadoParaPaciente(
                        nomeEstab,
                        tipoServico,
                        nomeProfissional,
                        novoInicioEmBrasilia),
                    corpoTexto: $"{nomeEstab} remarcou seu agendamento de {tipoServico} com {nomeProfissional} para {novoInicioEmBrasilia:dd/MM/yyyy 'às' HH:mm}. Em breve você poderá reconfirmar sua presença.");
            }
        }
        catch (Exception ex)
        {
            // R9: falha no envio não bloqueia o reagendamento (já persistido).
            // Sem destinatário/assunto/corpo no log (política LGPD/SES).
            _logger.LogWarning(ex, "Falha ao enviar e-mail de remarcação para agendamento {AgendamentoId}.", ev.AgendamentoId);
        }
    }
}
