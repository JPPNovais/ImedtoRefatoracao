using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

public class IniciarProntuarioCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long ModeloDeProntuarioId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
