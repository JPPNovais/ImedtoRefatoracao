using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Mapeamento entre colunas do arquivo de origem e schema canônico do Imedto.
/// Identidade: (MigracaoJobId, Entidade, NomeBlocoOrigem) — permite múltiplos blocos
/// do mesmo dump JSON classificados na mesma entidade canônica (ex: pacientes_antigos e
/// pacientes_novos, ambos classificados como "paciente").
/// Revisado pelo cliente antes de importar. Multi-tenant: sempre filtrado por EstabelecimentoId.
/// </summary>
public class MigracaoMapa : Entity
{
    public virtual long MigracaoJobId { get; protected set; }

    /// <summary>Redundante — necessário para queries diretas multi-tenant.</summary>
    public virtual long EstabelecimentoId { get; protected set; }

    /// <summary>Entidade canônica mapeada — ex.: "paciente", "agendamento".</summary>
    public virtual string Entidade { get; protected set; } = string.Empty;

    /// <summary>
    /// Nome da propriedade/bloco no dump de origem — ex.: "pacientes", "reparticoes", "pacientes_antigos".
    /// Parte da chave de identidade do mapa. Default vazio para dumps de arquivo único (CSV).
    /// </summary>
    public virtual string NomeBlocoOrigem { get; protected set; } = string.Empty;

    /// <summary>
    /// JSON do mapeamento: { "col_origem": "campo_canonico", "confianca": 0.95, "duvidas": ["col_x"],
    /// "entidade_classificada": "paciente", "confianca_classificacao": 0.92, "ignorado": false,
    /// "encoding_suspeito": false }
    /// </summary>
    public virtual string MapaJson { get; protected set; } = "{}";

    /// <summary>Usuário que revisou/aprovou o mapa. Null = revisão pendente.</summary>
    public virtual Guid? RevisadoPorUsuarioId { get; protected set; }

    /// <summary>Quando o mapa foi revisado/aprovado.</summary>
    public virtual DateTime? RevisadoEm { get; protected set; }

    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime AtualizadoEm { get; protected set; }

    protected MigracaoMapa() { }

    // ─── Factory ─────────────────────────────────────────────────────────────────

    public static MigracaoMapa Criar(
        long migracaoJobId,
        long estabelecimentoId,
        string entidade,
        string mapaJson,
        string nomeBlocoOrigem = "")
    {
        if (migracaoJobId <= 0) throw new BusinessException("Job é obrigatório.");
        if (estabelecimentoId <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(entidade)) throw new BusinessException("Entidade é obrigatória.");
        if (string.IsNullOrWhiteSpace(mapaJson)) throw new BusinessException("Mapa JSON é obrigatório.");

        var agora = DateTime.UtcNow;
        return new MigracaoMapa
        {
            MigracaoJobId = migracaoJobId,
            EstabelecimentoId = estabelecimentoId,
            Entidade = entidade.Trim(),
            NomeBlocoOrigem = (nomeBlocoOrigem ?? string.Empty).Trim(),
            MapaJson = mapaJson,
            CriadoEm = agora,
            AtualizadoEm = agora
        };
    }

    // ─── Comportamento ────────────────────────────────────────────────────────────

    /// <summary>
    /// Registra revisão do admin — atualiza o JSON e marca o revisor.
    /// </summary>
    public virtual void Revisar(string mapaJsonAtualizado, Guid revisadoPorUsuarioId)
    {
        if (string.IsNullOrWhiteSpace(mapaJsonAtualizado))
            throw new BusinessException("Mapa não pode ser vazio.");

        MapaJson = mapaJsonAtualizado;
        RevisadoPorUsuarioId = revisadoPorUsuarioId;
        RevisadoEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }
}
