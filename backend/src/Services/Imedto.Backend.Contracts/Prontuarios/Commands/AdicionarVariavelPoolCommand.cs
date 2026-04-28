using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

public class AdicionarVariavelPoolCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Tipo { get; set; }
    public string Nome { get; set; }
}
