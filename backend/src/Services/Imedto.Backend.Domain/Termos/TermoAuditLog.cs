using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Audit trail append-only para ações em termos (criar/editar modelo, emitir, anexar
/// PDF, revogar, aceite público, acesso a snapshot etc.). Filtro multi-tenant via
/// <see cref="EstabelecimentoId"/>; entradas de fluxo público (anônimo) podem ter o
/// estabelecimento preenchido — mas o <see cref="UsuarioId"/> fica null.
///
/// Lista fechada de ações (formato kebab-case):
///   "modelo-criado", "modelo-editado", "modelo-clonado", "modelo-ativado",
///   "modelo-desativado", "modelo-excluido", "termo-emitido", "termo-pdf-anexado",
///   "termo-revogado", "termo-aceite-publico", "termo-recusa-publica",
///   "termo-snapshot-visualizado".
/// </summary>
public class TermoAuditLog : Entity
{
    public virtual long? EstabelecimentoId { get; protected set; }
    public virtual Guid? UsuarioId { get; protected set; }
    public virtual string Acao { get; protected set; }
    public virtual string Entidade { get; protected set; }
    public virtual long EntidadeId { get; protected set; }
    public virtual string MetadataJson { get; protected set; }
    public virtual string IpOrigem { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    public const int AcaoMaximo = 60;
    public const int EntidadeMaximo = 40;

    protected TermoAuditLog() { }

    public static TermoAuditLog Registrar(
        long? estabelecimentoId,
        Guid? usuarioId,
        string acao,
        string entidade,
        long entidadeId,
        string metadataJson,
        string ipOrigem)
    {
        if (string.IsNullOrWhiteSpace(acao) || acao.Length > AcaoMaximo)
            throw new ArgumentException("Ação inválida.", nameof(acao));
        if (string.IsNullOrWhiteSpace(entidade) || entidade.Length > EntidadeMaximo)
            throw new ArgumentException("Entidade inválida.", nameof(entidade));

        return new TermoAuditLog
        {
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuarioId,
            Acao = acao.Trim(),
            Entidade = entidade.Trim(),
            EntidadeId = entidadeId,
            MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson,
            IpOrigem = string.IsNullOrWhiteSpace(ipOrigem) ? null : ipOrigem.Trim(),
            CriadoEm = DateTime.UtcNow,
        };
    }
}
