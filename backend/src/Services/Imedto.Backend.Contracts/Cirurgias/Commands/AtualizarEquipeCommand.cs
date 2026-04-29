using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cirurgias.Commands;

public class AtualizarEquipeCommand : ICommand
{
    public long ProcedimentoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public List<EquipeInicialPayload> Equipe { get; set; } = new();
}
