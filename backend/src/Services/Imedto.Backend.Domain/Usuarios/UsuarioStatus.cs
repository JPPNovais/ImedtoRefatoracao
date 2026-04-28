namespace Imedto.Backend.Domain.Usuarios;

public enum UsuarioStatus
{
    /// <summary>Conta criada, mas onboarding ainda não concluído.</summary>
    Pendente,

    /// <summary>Usuário com onboarding completo e acesso liberado.</summary>
    Ativo,

    /// <summary>Usuário desativado manualmente.</summary>
    Inativo
}
