using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Aggregate root de administrador global do Imedto.
/// Entidade separada de <c>Usuario</c> — admins não são usuários do app cliente.
/// JWT emitido para admin carrega claim <c>imedto_admin = "true"</c> e nunca <c>estabelecimento_id</c>.
/// </summary>
public class ImedtoAdmin : Entity<Guid>
{
    public virtual string Email { get; protected set; } = string.Empty;
    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual string SenhaHash { get; protected set; } = string.Empty;
    public virtual bool Ativo { get; protected set; }
    public virtual bool ForcePasswordReset { get; protected set; }
    public virtual DateTimeOffset CriadoEm { get; protected set; }
    public virtual DateTimeOffset? AtualizadoEm { get; protected set; }
    public virtual DateTimeOffset? UltimoLoginEm { get; protected set; }
    public virtual Guid? CriadoPorAdminId { get; protected set; }
    public virtual DateTimeOffset? DesativadoEm { get; protected set; }
    public virtual Guid? DesativadoPorAdminId { get; protected set; }

    protected ImedtoAdmin() { }

    public static ImedtoAdmin Criar(
        string email,
        string nome,
        string senhaHash,
        bool forcePasswordReset = true,
        Guid? criadoPorAdminId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessException("E-mail é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new BusinessException("Hash de senha é obrigatório.");

        return new ImedtoAdmin
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            Nome = nome.Trim(),
            SenhaHash = senhaHash,
            Ativo = true,
            ForcePasswordReset = forcePasswordReset,
            CriadoEm = DateTimeOffset.UtcNow,
            CriadoPorAdminId = criadoPorAdminId
        };
    }

    public virtual void RegistrarLogin()
    {
        UltimoLoginEm = DateTimeOffset.UtcNow;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public virtual void AtualizarSenha(string novoHash, bool forceReset = false)
    {
        if (string.IsNullOrWhiteSpace(novoHash))
            throw new BusinessException("Hash de senha é obrigatório.");

        SenhaHash = novoHash;
        ForcePasswordReset = forceReset;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public virtual void ConcluirResetSenha()
    {
        ForcePasswordReset = false;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public virtual void Desativar(Guid desativadoPorId)
    {
        if (!Ativo)
            throw new BusinessException("Admin já está desativado.");

        Ativo = false;
        DesativadoEm = DateTimeOffset.UtcNow;
        DesativadoPorAdminId = desativadoPorId;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public virtual void Reativar(Guid reativadoPorId)
    {
        if (Ativo)
            throw new BusinessException("Admin já está ativo.");

        Ativo = true;
        DesativadoEm = null;
        DesativadoPorAdminId = null;
        AtualizadoEm = DateTimeOffset.UtcNow;
    }

    public override string ToString() => $"ImedtoAdmin({Id}, {Email})";
}
