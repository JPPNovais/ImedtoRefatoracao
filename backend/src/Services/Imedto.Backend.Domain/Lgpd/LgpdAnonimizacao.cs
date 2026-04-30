using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Lgpd;

/// <summary>
/// Registro de auditoria de anonimização. Imutável após criação — não tem Update.
/// Grava quem anonimizou, o que e por qual motivo sem conter o dado original (LGPD).
/// </summary>
public class LgpdAnonimizacao : Entity
{
    public virtual string Tabela { get; protected set; }
    public virtual long RegistroId { get; protected set; }
    public virtual MotivoAnonimizacao Motivo { get; protected set; }
    public virtual DateTime AnonimizadoEm { get; protected set; }
    /// <summary>Null quando executado por job automático.</summary>
    public virtual Guid? ExecutadoPorUsuarioId { get; protected set; }

    protected LgpdAnonimizacao() { }

    public static LgpdAnonimizacao Registrar(
        string tabela,
        long registroId,
        MotivoAnonimizacao motivo,
        Guid? executadoPor)
    {
        if (string.IsNullOrWhiteSpace(tabela))
            throw new BusinessException("Tabela é obrigatória no registro de anonimização.");
        if (registroId <= 0)
            throw new BusinessException("Identificador do registro é inválido.");

        return new LgpdAnonimizacao
        {
            Tabela = tabela.Trim().ToLowerInvariant(),
            RegistroId = registroId,
            Motivo = motivo,
            AnonimizadoEm = DateTime.UtcNow,
            ExecutadoPorUsuarioId = executadoPor
        };
    }
}
