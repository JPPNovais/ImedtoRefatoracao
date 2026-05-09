using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Credencial de autenticação local. Substitui a integração com Supabase Auth.
/// O <see cref="Entity{TKey}.Id"/> é o mesmo do <c>public.usuarios.id</c> — relação 1:1
/// (por isso não há propriedade <c>UsuarioId</c> separada).
/// </summary>
public class AuthCredencial : Entity<Guid>
{
    public virtual string Email { get; protected set; }
    public virtual string SenhaHash { get; protected set; }
    public virtual DateTime? EmailConfirmadoEm { get; protected set; }
    public virtual DateTime? BloqueadoEm { get; protected set; }
    public virtual string MotivoBloqueio { get; protected set; }
    public virtual int TentativasFalhas { get; protected set; }
    public virtual DateTime? UltimoLoginEm { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    /// <summary>Após esta quantidade de falhas consecutivas, a conta é bloqueada.</summary>
    public const int MaxTentativasFalhas = 10;

    protected AuthCredencial() { }

    /// <summary>
    /// Cria credencial com senha já hasheada. O id deve ser o mesmo Guid do
    /// <see cref="Domain.Usuarios.Usuario"/> correspondente.
    /// </summary>
    public static AuthCredencial Criar(Guid id, string email, string senhaHash)
    {
        if (id == Guid.Empty)
            throw new BusinessException("Identificador é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessException("E-mail é obrigatório.");
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new BusinessException("Senha é obrigatória.");

        return new AuthCredencial
        {
            Id = id,
            Email = email.Trim().ToLowerInvariant(),
            SenhaHash = senhaHash,
            TentativasFalhas = 0,
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria credencial sem senha — usada em fluxo de convite. O usuário define
    /// senha via <see cref="DefinirSenha"/> ao consumir o token de convite.
    /// </summary>
    public static AuthCredencial CriarParaConvite(Guid id, string email)
    {
        if (id == Guid.Empty)
            throw new BusinessException("Identificador é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessException("E-mail é obrigatório.");

        return new AuthCredencial
        {
            Id = id,
            Email = email.Trim().ToLowerInvariant(),
            SenhaHash = null,
            TentativasFalhas = 0,
            CriadoEm = DateTime.UtcNow
        };
    }

    public virtual bool TemSenhaDefinida => !string.IsNullOrEmpty(SenhaHash);
    public virtual bool EmailConfirmado => EmailConfirmadoEm.HasValue;
    public virtual bool Bloqueado => BloqueadoEm.HasValue;

    public virtual void DefinirSenha(string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new BusinessException("Senha é obrigatória.");
        SenhaHash = senhaHash;
        TentativasFalhas = 0;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void RegistrarLoginBemSucedido()
    {
        UltimoLoginEm = DateTime.UtcNow;
        TentativasFalhas = 0;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Incrementa o contador de falhas. Se atingir <see cref="MaxTentativasFalhas"/>, bloqueia.</summary>
    public virtual void RegistrarFalhaLogin()
    {
        TentativasFalhas++;
        AtualizadoEm = DateTime.UtcNow;

        if (TentativasFalhas >= MaxTentativasFalhas)
            Bloquear($"Bloqueio automático após {MaxTentativasFalhas} tentativas de login falhas.");
    }

    public virtual void ConfirmarEmail()
    {
        if (EmailConfirmadoEm.HasValue) return;
        EmailConfirmadoEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Bloquear(string motivo)
    {
        if (BloqueadoEm.HasValue) return;
        BloqueadoEm = DateTime.UtcNow;
        MotivoBloqueio = motivo;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Desbloquear()
    {
        BloqueadoEm = null;
        MotivoBloqueio = null;
        TentativasFalhas = 0;
        AtualizadoEm = DateTime.UtcNow;
    }
}
