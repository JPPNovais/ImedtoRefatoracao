using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cirurgias.Commands;

public class RegistrarRealizacaoCommand : ICommand
{
    public long ProcedimentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public DateTime DataRealizada { get; set; }
    public string? DescricaoCirurgica { get; set; }
    public FichaAnestesica? FichaAnestesica { get; set; }
    public string? EvolucaoPosOp { get; set; }
}
