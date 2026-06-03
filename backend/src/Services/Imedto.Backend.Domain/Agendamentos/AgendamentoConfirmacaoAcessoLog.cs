using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Agendamentos;

/// <summary>
/// Log append-only de acessos públicos ao link de confirmação de agendamento.
/// Não contém PII do paciente — captura IP/UA para evidenciar o fluxo de confirmação (LGPD/auditoria).
///
/// Ações: <c>"visualizou_publico" | "confirmou_presenca"</c>.
/// </summary>
public class AgendamentoConfirmacaoAcessoLog : Entity
{
    public virtual long AgendamentoId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual string? IpOrigem { get; protected set; }
    public virtual string? UserAgent { get; protected set; }
    public virtual string Acao { get; protected set; } = string.Empty;
    public virtual DateTime AcessadoEm { get; protected set; }

    protected AgendamentoConfirmacaoAcessoLog() { }

    public static AgendamentoConfirmacaoAcessoLog Registrar(
        long agendamentoId,
        long estabelecimentoId,
        string? ipOrigem,
        string? userAgent,
        string acao)
    {
        if (agendamentoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(agendamentoId));
        if (estabelecimentoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(estabelecimentoId));
        if (string.IsNullOrWhiteSpace(acao))
            throw new ArgumentException("Ação obrigatória.", nameof(acao));

        var uaTrim = string.IsNullOrWhiteSpace(userAgent)
            ? null
            : (userAgent.Trim().Length > 500 ? userAgent.Trim()[..500] : userAgent.Trim());

        return new AgendamentoConfirmacaoAcessoLog
        {
            AgendamentoId = agendamentoId,
            EstabelecimentoId = estabelecimentoId,
            IpOrigem = string.IsNullOrWhiteSpace(ipOrigem) ? null : ipOrigem.Trim(),
            UserAgent = uaTrim,
            Acao = acao.Trim(),
            AcessadoEm = DateTime.UtcNow,
        };
    }
}
