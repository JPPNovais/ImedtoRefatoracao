using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Unidades.Commands;

public class DeletarUnidadeCommand : ICommand
{
    public long UnidadeId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
}
