using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

public class EditarModeloDescricaoCirurgicaCommand : ICommand
{
    public long ModeloId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Titulo { get; set; }
    public string Corpo { get; set; }
}
