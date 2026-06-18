using Dapper;
using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Time;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Imedto.Backend.Application.Automacoes.Commands;

public class EnviarLembretesAgendamentosCommandHandler : ICommandHandler<EnviarLembretesAgendamentosCommand>
{
    private readonly string _connStr;
    private readonly IEmailService _email;
    private readonly IWhatsappService _whatsapp;
    private readonly ILogger<EnviarLembretesAgendamentosCommandHandler> _logger;

    public EnviarLembretesAgendamentosCommandHandler(
        AppReadConnectionString conn,
        IEmailService email,
        IWhatsappService whatsapp,
        ILogger<EnviarLembretesAgendamentosCommandHandler> logger)
    {
        _connStr = conn.Value;
        _email = email;
        _whatsapp = whatsapp;
        _logger = logger;
    }

    public async Task Handle(EnviarLembretesAgendamentosCommand command)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        // CA1: agendamentos elegíveis para pelo menos um canal.
        // Multi-tenant: filtro estabelecimento_id via JOIN com configuracoes_automacao.
        // R5: inclui nome do estabelecimento para o corpo WhatsApp (identificação do tenant).
        var agendamentos = await conn.QueryAsync<LembreteRow>(
            """
            SELECT
                a.id,
                a.inicio_previsto                                                        AS InicioPrevisto,
                a.tipo_servico                                                           AS TipoServico,
                a.lembrete_por_email_enviado                                             AS LembretePorEmailEnviado,
                a.lembrete_por_whatsapp_enviado                                          AS LembretePorWhatsappEnviado,
                p.nome_completo                                                          AS PacienteNome,
                p.email                                                                  AS PacienteEmail,
                p.telefone                                                               AS PacienteTelefone,
                p.whatsapp_lembrete_opt_in                                               AS PacienteWhatsappOptIn,
                COALESCE(u.nome_completo, u.email)                                       AS ProfissionalNome,
                e.nome_fantasia                                                          AS NomeEstabelecimento,
                COALESCE(ca.lembretes_whatsapp_habilitados, false)                       AS LembretesWhatsappHabilitados
            FROM agendamentos a
            JOIN pacientes p ON p.id = a.paciente_id AND p.deletado_em IS NULL
            JOIN usuarios u ON u.id = a.profissional_usuario_id
            JOIN estabelecimentos e ON e.id = a.estabelecimento_id
            LEFT JOIN configuracoes_automacao ca ON ca.estabelecimento_id = a.estabelecimento_id
            WHERE a.status IN ('Agendado', 'Confirmado')
              AND (a.lembrete_por_email_enviado = false OR a.lembrete_por_whatsapp_enviado = false)
              AND a.inicio_previsto >= NOW()
              AND a.inicio_previsto <= NOW() + (COALESCE(ca.horas_antecedencia_lembrete, 24) * INTERVAL '1 hour')
              AND COALESCE(ca.lembretes_habilitados, false) = true
            """);

        foreach (var ag in agendamentos)
        {
            var hora = ag.InicioPrevisto.ToBrasilia().ToString("dd/MM/yyyy 'às' HH:mm");

            // CA2: e-mail continua exatamente igual — comportamento não pode regredir.
            if (!ag.LembretePorEmailEnviado && ag.PacienteEmail != null)
            {
                var assunto = $"Lembrete: consulta de {ag.TipoServico} em {hora}";
                var corpo = $"""
                    <p>Olá, <strong>{ag.PacienteNome}</strong>!</p>
                    <p>Este é um lembrete da sua consulta:</p>
                    <ul>
                        <li><strong>Serviço:</strong> {ag.TipoServico}</li>
                        <li><strong>Profissional:</strong> {ag.ProfissionalNome}</li>
                        <li><strong>Data/hora:</strong> {hora}</li>
                    </ul>
                    <p>Caso precise reagendar, entre em contato com o estabelecimento.</p>
                    """;

                await _email.EnviarAsync(ag.PacienteEmail!, assunto, corpo);

                await conn.ExecuteAsync(
                    "UPDATE agendamentos SET lembrete_por_email_enviado = true WHERE id = @Id",
                    new { ag.Id });
            }

            // CA1/CA3/R2: WhatsApp só quando todos os gates passam.
            if (!ag.LembretePorWhatsappEnviado
                && ag.LembretesWhatsappHabilitados
                && ag.PacienteWhatsappOptIn
                && TentarNormalizarE164(ag.PacienteTelefone, out var foneE164))
            {
                // R7/CA10: falha de entrega não derruba o lote nem o canal de e-mail.
                try
                {
                    // R5: variáveis do template na ordem declarada no template aprovado.
                    // Texto: "Olá, {{nome_paciente}}! Lembrete da sua consulta na
                    // {{nome_estabelecimento}}: {{tipo_servico}} com {{nome_profissional}}
                    // em {{data_hora}}. Em caso de dúvida, entre em contato com o estabelecimento."
                    var variaveis = new[]
                    {
                        ag.PacienteNome,
                        ag.NomeEstabelecimento,
                        ag.TipoServico,
                        ag.ProfissionalNome,
                        hora
                    };

                    await _whatsapp.EnviarTemplateAsync(foneE164!, variaveis);

                    // CA12: marcar idempotência apenas após sucesso.
                    await conn.ExecuteAsync(
                        "UPDATE agendamentos SET lembrete_por_whatsapp_enviado = true WHERE id = @Id",
                        new { ag.Id });
                }
                catch (Exception ex)
                {
                    // CA9/R7: LogWarning sem PII — apenas a exceção técnica.
                    _logger.LogWarning(ex,
                        "[Lembretes] Falha ao enviar WhatsApp para agendamento {AgendamentoId}. Será tentado novamente na próxima rodada.",
                        ag.Id);
                    // Não relança — próxima rodada tentará novamente (não marcou enviado).
                }
            }
        }
    }

    /// <summary>
    /// Normaliza o telefone armazenado (digits-only, sanitizado pelo domain) para E.164.
    /// Assume Brasil (+55) quando DDI ausente. Retorna false para telefone inválido (CA4/R2.d).
    /// </summary>
    internal static bool TentarNormalizarE164(string? telefone, out string? resultado)
    {
        resultado = null;

        if (string.IsNullOrWhiteSpace(telefone)) return false;

        // Já vem digits-only do Paciente.Telefone (SanitizeOpt com digitsOnly:true).
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());

        // Aceita:
        //   11 dígitos: DDD (2) + número (9 dígitos) — ex: 11999999999
        //   10 dígitos: DDD (2) + número (8 dígitos) — fixo
        //   13 dígitos: DDI 55 + DDD (2) + número (9 dígitos)
        //   12 dígitos: DDI 55 + DDD (2) + número (8 dígitos)
        if (digitos.Length == 11 || digitos.Length == 10)
        {
            resultado = "+55" + digitos;
            return true;
        }

        if ((digitos.Length == 13 || digitos.Length == 12) && digitos.StartsWith("55"))
        {
            resultado = "+" + digitos;
            return true;
        }

        return false;
    }

    private sealed class LembreteRow
    {
        public long Id { get; set; }
        public DateTime InicioPrevisto { get; set; }
        public string TipoServico { get; set; } = string.Empty;
        public bool LembretePorEmailEnviado { get; set; }
        public bool LembretePorWhatsappEnviado { get; set; }
        public string PacienteNome { get; set; } = string.Empty;
        public string? PacienteEmail { get; set; }
        public string? PacienteTelefone { get; set; }
        public bool PacienteWhatsappOptIn { get; set; }
        public string ProfissionalNome { get; set; } = string.Empty;
        public string NomeEstabelecimento { get; set; } = string.Empty;
        public bool LembretesWhatsappHabilitados { get; set; }
    }
}
