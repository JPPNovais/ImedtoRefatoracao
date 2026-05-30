using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Variável pool global gerenciada pelo admin do sistema.
/// Tabela global — sem estabelecimento_id. Tenants importam via cópia independente.
///
/// Nome da tabela no Postgres: <c>imedto_variavel_pool_global</c>.
/// Índices: unique LOWER(nome), (ativo, tipo, nome) para listagem filtrada.
/// </summary>
public class ImedtoVariavelPoolGlobal : Entity<Guid>
{
    public virtual string Nome { get; protected set; } = string.Empty;

    /// <summary>
    /// Tipo da variável: 'texto', 'numerico', 'data', 'lista', 'booleano'.
    /// Armazenado como text no Postgres (enum-string sem extensão enum SQL).
    /// </summary>
    public virtual string Tipo { get; protected set; } = string.Empty;

    /// <summary>
    /// Valores possíveis — apenas para tipo 'lista'.
    /// Armazenado como JSONB (array de strings). Nulo para outros tipos.
    /// Exemplo: '["Normal","Hipertensão Estágio 1","Hipertensão Estágio 2"]'
    /// </summary>
    public virtual string? ValoresJson { get; protected set; }

    public virtual string? Descricao { get; protected set; }
    public virtual bool Ativo { get; protected set; } = true;
    public virtual DateTimeOffset CriadoEm { get; protected set; }
    public virtual DateTimeOffset? AtualizadoEm { get; protected set; }
    public virtual Guid? CriadoPorAdminId { get; protected set; }
    public virtual Guid? AtualizadoPorAdminId { get; protected set; }

    // Tipos permitidos (enum-string).
    public static readonly IReadOnlySet<string> TiposPermitidos =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "texto", "numerico", "data", "lista", "booleano" };

    protected ImedtoVariavelPoolGlobal() { }

    public static ImedtoVariavelPoolGlobal Criar(
        string nome,
        string tipo,
        string? valoresJson,
        string? descricao,
        Guid? criadoPorAdminId)
    {
        ValidarNome(nome);
        ValidarTipo(tipo);
        ValidarValoresJson(tipo, valoresJson);

        return new ImedtoVariavelPoolGlobal
        {
            Id = Guid.NewGuid(),
            Nome = nome.Trim(),
            Tipo = tipo.Trim().ToLowerInvariant(),
            ValoresJson = valoresJson,
            Descricao = descricao?.Trim(),
            Ativo = true,
            CriadoEm = DateTimeOffset.UtcNow,
            AtualizadoEm = DateTimeOffset.UtcNow,
            CriadoPorAdminId = criadoPorAdminId,
            AtualizadoPorAdminId = criadoPorAdminId
        };
    }

    public virtual void Atualizar(
        string nome,
        string tipo,
        string? valoresJson,
        string? descricao,
        Guid? atualizadoPorAdminId)
    {
        ValidarNome(nome);
        ValidarTipo(tipo);
        ValidarValoresJson(tipo, valoresJson);
        Nome = nome.Trim();
        Tipo = tipo.Trim().ToLowerInvariant();
        ValoresJson = valoresJson;
        Descricao = descricao?.Trim();
        AtualizadoEm = DateTimeOffset.UtcNow;
        AtualizadoPorAdminId = atualizadoPorAdminId;
    }

    public virtual void Desativar(Guid? atualizadoPorAdminId)
    {
        Ativo = false;
        AtualizadoEm = DateTimeOffset.UtcNow;
        AtualizadoPorAdminId = atualizadoPorAdminId;
    }

    public virtual void Reativar(Guid? atualizadoPorAdminId)
    {
        Ativo = true;
        AtualizadoEm = DateTimeOffset.UtcNow;
        AtualizadoPorAdminId = atualizadoPorAdminId;
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome da variável é obrigatório.");
        if (nome.Trim().Length > 200)
            throw new BusinessException("Nome da variável não pode exceder 200 caracteres.");
    }

    private static void ValidarTipo(string tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            throw new BusinessException("Tipo da variável é obrigatório.");
        if (!TiposPermitidos.Contains(tipo.Trim()))
            throw new BusinessException(
                $"Tipo '{tipo}' inválido. Valores permitidos: {string.Join(", ", TiposPermitidos)}.");
    }

    private static void ValidarValoresJson(string tipo, string? valoresJson)
    {
        if (tipo.Trim().Equals("lista", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(valoresJson))
            throw new BusinessException("Variáveis do tipo 'lista' exigem ao menos um valor em valores_json.");
    }
}
