using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Configuração global chave-valor do sistema.
///
/// Chave é a PK (text, formato "secao.nome"). Valor é JSONB para suportar qualquer tipo.
/// Tipo controla a validação e o widget de UI: numerico, texto, email, toggle.
/// Secao agrupa chaves na tela de configurações (ex: "Trial", "Comunicação").
/// </summary>
public class ImedtoConfig : Entity<string>
{
    public virtual string Valor { get; protected set; } = "null";
    public virtual string Tipo { get; protected set; } = "texto";
    public virtual string? Secao { get; protected set; }
    public virtual string? Descricao { get; protected set; }
    public virtual DateTimeOffset AtualizadoEm { get; protected set; }
    public virtual Guid? AtualizadoPorAdminId { get; protected set; }

    // Tipos permitidos (enum-string).
    public static readonly IReadOnlySet<string> TiposPermitidos =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "numerico", "texto", "email", "toggle" };

    protected ImedtoConfig() { }

    public static ImedtoConfig Criar(
        string chave,
        string valorJson,
        string tipo,
        string? secao,
        string? descricao,
        Guid? atualizadoPorAdminId)
    {
        ValidarChave(chave);
        ValidarTipo(tipo);
        if (string.IsNullOrWhiteSpace(valorJson))
            throw new BusinessException("Valor JSON é obrigatório.");

        return new ImedtoConfig
        {
            Id = chave.Trim(),
            Valor = valorJson,
            Tipo = tipo.Trim().ToLowerInvariant(),
            Secao = secao?.Trim(),
            Descricao = descricao?.Trim(),
            AtualizadoEm = DateTimeOffset.UtcNow,
            AtualizadoPorAdminId = atualizadoPorAdminId
        };
    }

    public virtual void Atualizar(string valorJson, Guid? atualizadoPorAdminId)
    {
        if (string.IsNullOrWhiteSpace(valorJson))
            throw new BusinessException("Valor JSON é obrigatório.");
        Valor = valorJson;
        AtualizadoEm = DateTimeOffset.UtcNow;
        AtualizadoPorAdminId = atualizadoPorAdminId;
    }

    private static void ValidarChave(string chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
            throw new BusinessException("Chave é obrigatória.");
        if (chave.Length > 100)
            throw new BusinessException("Chave não pode exceder 100 caracteres.");
    }

    private static void ValidarTipo(string tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            throw new BusinessException("Tipo de configuração é obrigatório.");
        if (!TiposPermitidos.Contains(tipo.Trim()))
            throw new BusinessException(
                $"Tipo '{tipo}' inválido. Valores permitidos: {string.Join(", ", TiposPermitidos)}.");
    }
}
