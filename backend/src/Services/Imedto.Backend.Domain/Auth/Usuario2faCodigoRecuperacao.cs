using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Código de recuperação one-time para 2FA. 1:N com <see cref="Usuario2fa"/>.
///
/// O código nunca é persistido em claro — <see cref="CodigoHash"/> contém o hash
/// produzido pelo hasher de senha do projeto (mesmo algoritmo de <see cref="AuthCredencial.SenhaHash"/>).
/// Cada código é válido exatamente uma vez; após uso, <see cref="UsadoEm"/> é preenchido
/// e o código jamais autentica novamente.
/// </summary>
public class Usuario2faCodigoRecuperacao : Entity<long>
{
    public virtual Guid UsuarioId { get; protected set; }

    /// <summary>
    /// Hash do código de recuperação (algoritmo de senha do projeto — BCrypt + HMAC-SHA256).
    /// Nunca persistir o código em claro.
    /// </summary>
    public virtual string CodigoHash { get; protected set; }

    /// <summary>Quando o código foi consumido. Null = ainda disponível.</summary>
    public virtual DateTime? UsadoEm { get; protected set; }

    public virtual DateTime CriadoEm { get; protected set; }

    public virtual bool JaUsado => UsadoEm.HasValue;

    protected Usuario2faCodigoRecuperacao() { }

    /// <summary>Cria um código de recuperação já hasheado.</summary>
    public static Usuario2faCodigoRecuperacao Criar(Guid usuarioId, string codigoHash)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("UsuarioId é obrigatório.");
        if (string.IsNullOrWhiteSpace(codigoHash))
            throw new BusinessException("Hash do código é obrigatório.");

        return new Usuario2faCodigoRecuperacao
        {
            UsuarioId = usuarioId,
            CodigoHash = codigoHash,
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>Marca o código como usado. Idempotente — não relança se já marcado.</summary>
    public virtual void Consumir()
    {
        if (UsadoEm.HasValue) return;
        UsadoEm = DateTime.UtcNow;
    }
}
