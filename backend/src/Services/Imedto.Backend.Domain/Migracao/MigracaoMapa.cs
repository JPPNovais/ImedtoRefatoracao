using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// POCO de mapeamento EF para mapa de campos entre origem e schema canônico.
/// Um mapa por entidade por job. Revisado pelo cliente antes de importar.
/// Aggregate root completo será construído pelo developer.
/// </summary>
public class MigracaoMapa : Entity
{
    public virtual long MigracaoJobId { get; protected set; }

    /// <summary>Redundante — necessário para queries diretas multi-tenant.</summary>
    public virtual long EstabelecimentoId { get; protected set; }

    /// <summary>Entidade mapeada — ex.: "paciente", "agendamento".</summary>
    public virtual string Entidade { get; protected set; } = string.Empty;

    /// <summary>
    /// JSON do mapeamento: { "col_origem": "campo_canonico", "confianca": 0.95, "duvidas": ["col_x"] }
    /// </summary>
    public virtual string MapaJson { get; protected set; } = "{}";

    /// <summary>Usuário que revisou/aprovou o mapa. Null = revisão pendente.</summary>
    public virtual Guid? RevisadoPorUsuarioId { get; protected set; }

    /// <summary>Quando o mapa foi revisado/aprovado.</summary>
    public virtual DateTime? RevisadoEm { get; protected set; }

    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime AtualizadoEm { get; protected set; }

    protected MigracaoMapa() { }
}
