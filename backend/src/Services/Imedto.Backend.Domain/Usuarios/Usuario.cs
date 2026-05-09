using Imedto.Backend.Domain.Usuarios.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Usuarios;

/// <summary>
/// Aggregate root de Usuário. O Id é o mesmo UUID da credencial em <c>auth_credenciais</c> —
/// a tabela <c>public.usuarios</c> tem FK com <c>ON DELETE CASCADE</c> para a credencial.
/// </summary>
public class Usuario : Entity<Guid>
{
    public virtual string Email { get; protected set; }
    public virtual string NomeCompleto { get; protected set; }
    public virtual string Cpf { get; protected set; }
    public virtual string Telefone { get; protected set; }
    public virtual UsuarioStatus Status { get; protected set; }
    public virtual bool OnboardingCompleto { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }
    public virtual DateTime? UltimoAcessoEm { get; protected set; }

    protected Usuario() { }

    public static Usuario Criar(Guid id, string email)
    {
        if (id == Guid.Empty)
            throw new BusinessException("Identificador do usuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessException("E-mail é obrigatório.");

        var usuario = new Usuario
        {
            Id = id,
            Email = email.Trim().ToLowerInvariant(),
            Status = UsuarioStatus.Pendente,
            OnboardingCompleto = false,
            CriadoEm = DateTime.UtcNow
        };

        usuario.AddDomainEvent(new UsuarioCriadoEvent(usuario.Id, usuario.Email));
        return usuario;
    }

    public virtual void CompletarOnboarding(string nomeCompleto, string cpf, string telefone)
    {
        PreencherPerfil(nomeCompleto, cpf, telefone);
        MarcarOnboardingCompleto();
    }

    /// <summary>Salva nome/CPF/telefone sem marcar o onboarding como concluído.</summary>
    public virtual void PreencherPerfil(string nomeCompleto, string cpf, string telefone)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto))
            throw new BusinessException("Nome completo é obrigatório.");
        if (string.IsNullOrWhiteSpace(cpf))
            throw new BusinessException("CPF é obrigatório.");

        var cpfDigitos = SomenteDigitos(cpf);
        if (cpfDigitos.Length != 11)
            throw new BusinessException("CPF deve conter 11 dígitos.");

        NomeCompleto = nomeCompleto.Trim();
        Cpf = cpfDigitos;
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : SomenteDigitos(telefone);
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>Ativa o usuário e marca onboarding como concluído. Chamar apenas após salvar todos os dados.</summary>
    public virtual void MarcarOnboardingCompleto()
    {
        Status = UsuarioStatus.Ativo;
        OnboardingCompleto = true;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void AtualizarPerfil(string nomeCompleto, string telefone)
    {
        if (!string.IsNullOrWhiteSpace(nomeCompleto))
            NomeCompleto = nomeCompleto.Trim();

        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : SomenteDigitos(telefone);
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void RegistrarAcesso()
    {
        UltimoAcessoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Anonimiza PII da conta do titular (direito ao esquecimento, Art. 18 LGPD).
    /// O Id é mantido para preservar integridade referencial com registros históricos.
    /// TODO: revogar refresh tokens da credencial após anonimizar — feito pelo controller.
    /// </summary>
    public virtual void Anonimizar()
    {
        NomeCompleto = $"Usuário Anonimizado";
        Cpf = null;
        Telefone = null;
        Status = UsuarioStatus.Inativo;
        AtualizadoEm = DateTime.UtcNow;
        // E-mail: não nulificamos aqui pois é chave de identificação na credencial de auth.
        // O controller deve revogar o refresh token + o frontend redirecionar para logout.
        // Se necessário anonimizar o e-mail no futuro, fazer via job que aguarda a sessão expirar.
    }

    private static string SomenteDigitos(string valor) =>
        new(valor.Where(char.IsDigit).ToArray());
}
