using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Commands;

public class CancelarReceitaCommand : ICommand
{
    public long ReceitaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
