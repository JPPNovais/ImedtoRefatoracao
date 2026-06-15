using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// POCO de mapeamento EF para template de mapeamento cross-tenant.
/// Templates são metadados de schema (não dados de paciente) — sem estabelecimento_id.
/// Justificativa global: compartilhado entre todos os tenants como ponto de partida de IA.
/// Aggregate root completo será construído pelo developer.
/// </summary>
public class MigracaoTemplate : Entity
{
    /// <summary>Nome do sistema de origem — ex.: "iClinic", "Feegow".</summary>
    public virtual string Nome { get; protected set; } = string.Empty;

    /// <summary>Entidade mapeada — ex.: "paciente", "agendamento".</summary>
    public virtual string Entidade { get; protected set; } = string.Empty;

    /// <summary>JSON do mapeamento canônico.</summary>
    public virtual string MapaJson { get; protected set; } = "{}";

    /// <summary>Usuário (admin) que criou o template.</summary>
    public virtual Guid CriadoPorUsuarioId { get; protected set; }

    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime AtualizadoEm { get; protected set; }

    protected MigracaoTemplate() { }
}
