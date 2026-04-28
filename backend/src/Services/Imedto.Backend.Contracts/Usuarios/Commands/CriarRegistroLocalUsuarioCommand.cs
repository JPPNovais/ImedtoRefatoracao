using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Usuarios.Commands;

/// <summary>
/// Cria o registro de domínio em <c>public.usuarios</c> após um signup bem-sucedido no Supabase Auth.
/// O <see cref="Id"/> é o mesmo UUID do <c>auth.users</c>.
/// </summary>
public class CriarRegistroLocalUsuarioCommand : ICommand
{
    public Guid Id { get; set; }
    public string Email { get; set; }
}
