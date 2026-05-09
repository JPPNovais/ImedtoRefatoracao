using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Usuarios.Commands;

/// <summary>
/// Cria o registro de domínio em <c>public.usuarios</c> após um signup bem-sucedido.
/// O <see cref="Id"/> é o mesmo UUID da credencial em <c>auth_credenciais</c>.
/// </summary>
public class CriarRegistroLocalUsuarioCommand : ICommand
{
    public Guid Id { get; set; }
    public string Email { get; set; }
}
