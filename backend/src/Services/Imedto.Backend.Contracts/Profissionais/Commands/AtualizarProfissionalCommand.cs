using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Profissionais.Commands;

public class AtualizarProfissionalCommand : ICommand
{
    public Guid UsuarioId { get; set; }
    public string Conselho { get; set; }
    public string Uf { get; set; }
    public string NumeroRegistro { get; set; }
    public string Especialidade { get; set; }
    public string Bio { get; set; }
}
