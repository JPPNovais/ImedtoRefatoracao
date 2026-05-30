using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Catálogo global de regiões anatômicas de alto nível, gerenciado pelo admin do sistema.
/// Tabela global — sem estabelecimento_id.
///
/// Propósito: catálogo editável de regiões usadas em formulários clínicos do tenant
/// (ex: região alvo de procedimento, área de exame). Distinto de
/// <c>RegiaoAnatomicaCatalogo</c> que é o mapa hierárquico detalhado do exame físico interativo.
///
/// Nome da tabela no Postgres: <c>imedto_regiao_anatomica_global</c>.
/// Índices: unique LOWER(nome), GIN em sinonimos, (ativo, sistema_corporal, nome).
/// </summary>
public class ImedtoRegiaoAnatomicaGlobal : Entity<Guid>
{
    public virtual string Nome { get; protected set; } = string.Empty;

    /// <summary>
    /// Array de sinônimos para busca (ex: ["cabeça", "crânio"]).
    /// Armazenado como text[] no Postgres. Nulo equivale a array vazio.
    /// </summary>
    public virtual string[]? Sinonimos { get; protected set; }

    /// <summary>
    /// Sistema corporal ao qual a região pertence.
    /// Valores sugeridos: musculoesqueletico, cardiovascular, neurologico, tegumentar, geral, etc.
    /// Texto livre — não é enum SQL.
    /// </summary>
    public virtual string? SistemaCorporal { get; protected set; }

    public virtual bool Ativo { get; protected set; } = true;
    public virtual DateTimeOffset CriadoEm { get; protected set; }
    public virtual DateTimeOffset? AtualizadoEm { get; protected set; }

    protected ImedtoRegiaoAnatomicaGlobal() { }

    public static ImedtoRegiaoAnatomicaGlobal Criar(
        string nome,
        string[]? sinonimos,
        string? sistemaCorporal)
    {
        ValidarNome(nome);

        return new ImedtoRegiaoAnatomicaGlobal
        {
            Id = Guid.NewGuid(),
            Nome = nome.Trim(),
            Sinonimos = sinonimos,
            SistemaCorporal = sistemaCorporal?.Trim().ToLowerInvariant(),
            Ativo = true,
            CriadoEm = DateTimeOffset.UtcNow,
            AtualizadoEm = DateTimeOffset.UtcNow
        };
    }

    public virtual void Atualizar(string nome, string[]? sinonimos, string? sistemaCorporal)
    {
        ValidarNome(nome);
        Nome = nome.Trim();
        Sinonimos = sinonimos;
        SistemaCorporal = sistemaCorporal?.Trim().ToLowerInvariant();
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public virtual void Desativar()
    {
        Ativo = false;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public virtual void Reativar()
    {
        Ativo = true;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da região anatômica é obrigatório.");
        if (nome.Trim().Length > 200)
            throw new BusinessException("Nome da região anatômica não pode exceder 200 caracteres.");
    }
}
