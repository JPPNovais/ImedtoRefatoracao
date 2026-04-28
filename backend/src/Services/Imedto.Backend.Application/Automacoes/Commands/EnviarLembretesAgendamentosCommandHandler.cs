using Dapper;
using Imedto.Backend.Contracts.Automacoes.Commands;
using Imedto.Backend.Domain.Automacoes;
using Imedto.Backend.Infrastructure;
using Imedto.Backend.SharedKernel.Cqrs;
using Npgsql;

namespace Imedto.Backend.Application.Automacoes.Commands;

public class EnviarLembretesAgendamentosCommandHandler : ICommandHandler<EnviarLembretesAgendamentosCommand>
{
    private readonly string _connStr;
    private readonly IEmailService _email;

    public EnviarLembretesAgendamentosCommandHandler(AppReadConnectionString conn, IEmailService email)
    {
        _connStr = conn.Value;
        _email = email;
    }

    public async Task Handle(EnviarLembretesAgendamentosCommand command)
    {
        await using var conn = new NpgsqlConnection(_connStr);

        var agendamentos = await conn.QueryAsync<LembreteRow>(
            """
            SELECT
                a.id,
                a.inicio_previsto AS InicioPrevisto,
                a.tipo_servico AS TipoServico,
                p.nome_completo AS PacienteNome,
                p.email AS PacienteEmail,
                COALESCE(u.nome_completo, u.email) AS ProfissionalNome
            FROM agendamentos a
            JOIN pacientes p ON p.id = a.paciente_id AND p.deletado_em IS NULL
            JOIN usuarios u ON u.id = a.profissional_usuario_id
            LEFT JOIN configuracoes_automacao ca ON ca.estabelecimento_id = a.estabelecimento_id
            WHERE a.status IN ('Agendado', 'Confirmado')
              AND a.lembrete_por_email_enviado = false
              AND a.inicio_previsto >= NOW()
              AND a.inicio_previsto <= NOW() + (COALESCE(ca.horas_antecedencia_lembrete, 24) * INTERVAL '1 hour')
              AND COALESCE(ca.lembretes_habilitados, false) = true
              AND p.email IS NOT NULL
            """);

        foreach (var ag in agendamentos)
        {
            var hora = ag.InicioPrevisto.ToLocalTime().ToString("dd/MM/yyyy 'às' HH:mm");
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
    }

    private sealed class LembreteRow
    {
        public long Id { get; set; }
        public DateTime InicioPrevisto { get; set; }
        public string TipoServico { get; set; } = string.Empty;
        public string PacienteNome { get; set; } = string.Empty;
        public string? PacienteEmail { get; set; }
        public string ProfissionalNome { get; set; } = string.Empty;
    }
}
