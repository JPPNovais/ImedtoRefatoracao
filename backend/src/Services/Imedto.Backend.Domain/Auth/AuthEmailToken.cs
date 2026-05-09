using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Tipos de token de e-mail usados em fluxos fora do login (signup, reset de senha, convite).
/// </summary>
public enum AuthEmailTokenTipo
{
    /// <summary>Confirmação de e-mail recém-cadastrado.</summary>
    ConfirmacaoEmail = 1,
    /// <summary>Redefinição de senha (esqueci a senha).</summary>
    ResetSenha = 2,
    /// <summary>Convite a profissional (cria credencial e define senha em um único fluxo).</summary>
    Convite = 3
}

/// <summary>
/// Token de uso único persistido como SHA-256 do token cru. Usado em fluxos
/// que disparam e-mail (confirmação, reset, convite).
/// </summary>
public class AuthEmailToken : Entity<long>
{
    public virtual Guid UsuarioId { get; protected set; }
    public virtual AuthEmailTokenTipo Tipo { get; protected set; }
    public virtual string TokenHash { get; protected set; }
    public virtual DateTime ExpiraEm { get; protected set; }
    public virtual DateTime? ConsumidoEm { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }

    protected AuthEmailToken() { }

    public static AuthEmailToken Emitir(
        Guid usuarioId,
        AuthEmailTokenTipo tipo,
        string tokenHash,
        DateTime expiraEm)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("UsuarioId é obrigatório.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new BusinessException("Hash do token é obrigatório.");
        if (expiraEm <= DateTime.UtcNow)
            throw new BusinessException("Expiração deve ser no futuro.");

        return new AuthEmailToken
        {
            UsuarioId = usuarioId,
            Tipo = tipo,
            TokenHash = tokenHash,
            ExpiraEm = expiraEm,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual bool Expirado => ExpiraEm <= DateTime.UtcNow;
    public virtual bool Consumido => ConsumidoEm.HasValue;
    public virtual bool Valido => !Expirado && !Consumido;

    public virtual void MarcarComoConsumido()
    {
        if (ConsumidoEm.HasValue)
            throw new BusinessException("Este token já foi utilizado.");
        ConsumidoEm = DateTime.UtcNow;
    }
}
