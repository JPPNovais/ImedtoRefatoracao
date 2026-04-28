using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.ModelosPermissao.Commands;

public class ExcluirModeloPermissaoCommand : ICommand
{
    public long ModeloId { get; set; }
    public long EstabelecimentoId { get; set; }
}
