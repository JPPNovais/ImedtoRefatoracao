using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Template de mapeamento cross-tenant reutilizável.
/// Templates são metadados de schema (não dados de paciente) — sem estabelecimento_id.
/// Compartilhado entre todos os tenants como ponto de partida para a inferência IA.
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

    // ─── Factory ─────────────────────────────────────────────────────────────────

    public static MigracaoTemplate Criar(string nome, string entidade, string mapaJson, Guid criadoPorUsuarioId)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new BusinessException("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(entidade)) throw new BusinessException("Entidade é obrigatória.");
        if (string.IsNullOrWhiteSpace(mapaJson)) throw new BusinessException("Mapa JSON é obrigatório.");
        if (criadoPorUsuarioId == Guid.Empty) throw new BusinessException("Usuário criador é obrigatório.");

        var agora = DateTime.UtcNow;
        return new MigracaoTemplate
        {
            Nome = nome.Trim(),
            Entidade = entidade.Trim(),
            MapaJson = mapaJson,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            CriadoEm = agora,
            AtualizadoEm = agora
        };
    }

    // ─── Comportamento ────────────────────────────────────────────────────────────

    public virtual void AtualizarMapa(string novoMapaJson)
    {
        if (string.IsNullOrWhiteSpace(novoMapaJson))
            throw new BusinessException("Mapa JSON não pode ser vazio.");

        MapaJson = novoMapaJson;
        AtualizadoEm = DateTime.UtcNow;
    }
}
