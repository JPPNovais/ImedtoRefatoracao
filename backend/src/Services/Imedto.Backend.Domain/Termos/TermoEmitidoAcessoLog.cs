using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Log append-only de acessos públicos ao termo (fluxo de aceite anônimo via token).
/// Não é PII clínico — captura IP/UA pra evidenciar fluxo de aceite (LGPD/contrato).
///
/// Ações: <c>"visualizou_publico" | "aceitou" | "recusou"</c>.
/// </summary>
public class TermoEmitidoAcessoLog : Entity
{
    public virtual long TermoEmitidoId { get; protected set; }
    public virtual string IpOrigem { get; protected set; }
    public virtual string UserAgent { get; protected set; }
    public virtual string Acao { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    protected TermoEmitidoAcessoLog() { }

    public static TermoEmitidoAcessoLog Registrar(
        long termoEmitidoId,
        string ipOrigem,
        string userAgent,
        string acao)
    {
        if (termoEmitidoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(termoEmitidoId));
        if (string.IsNullOrWhiteSpace(acao))
            throw new ArgumentException("Ação obrigatória.", nameof(acao));

        var uaTrim = string.IsNullOrWhiteSpace(userAgent)
            ? null
            : (userAgent.Trim().Length > 500 ? userAgent.Trim()[..500] : userAgent.Trim());

        return new TermoEmitidoAcessoLog
        {
            TermoEmitidoId = termoEmitidoId,
            IpOrigem = string.IsNullOrWhiteSpace(ipOrigem) ? null : ipOrigem.Trim(),
            UserAgent = uaTrim,
            Acao = acao.Trim(),
            CriadoEm = DateTime.UtcNow,
        };
    }
}
