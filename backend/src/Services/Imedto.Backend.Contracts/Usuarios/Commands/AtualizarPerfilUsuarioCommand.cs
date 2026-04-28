using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Usuarios.Commands;

public class AtualizarPerfilUsuarioCommand : ICommand
{
    public Guid UsuarioId { get; set; }
    public string NomeCompleto { get; set; }
    public string Telefone { get; set; }
}
