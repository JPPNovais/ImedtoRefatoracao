using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

public class ExcluirModeloDescricaoCirurgicaCommand : ICommand
{
    public long ModeloId { get; set; }
    public long EstabelecimentoId { get; set; }
}
