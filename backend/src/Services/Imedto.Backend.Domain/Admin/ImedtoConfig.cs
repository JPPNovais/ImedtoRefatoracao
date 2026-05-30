using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Configuração global chave-valor do sistema. MVP entrega só a tabela + leitura via
/// <c>IImedtoConfigReader</c>. Sem UI dedicada (uso interno por outros serviços).
///
/// Chave é a PK (text). Valor é JSONB para suportar qualquer tipo (string, número, objeto).
/// Exemplos de chave: "smtp.override", "feature.flags", "trial.duracao_dias".
/// </summary>
public class ImedtoConfig : Entity<string>
{
    public virtual string Valor { get; protected set; } = "null";
    public virtual string? Descricao { get; protected set; }
    public virtual DateTimeOffset AtualizadoEm { get; protected set; }
    public virtual Guid? AtualizadoPorAdminId { get; protected set; }

    protected ImedtoConfig() { }

    public static ImedtoConfig Criar(
        string chave,
        string valorJson,
        string? descricao,
        Guid? atualizadoPorAdminId)
    {
        ValidarChave(chave);
        if (string.IsNullOrWhiteSpace(valorJson))
            throw new BusinessException("Valor JSON é obrigatório.");

        return new ImedtoConfig
        {
            Id = chave.Trim(),
            Valor = valorJson,
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
}
