using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Salas.Commands;

public class AtualizarSalaCommand : ICommand
{
    public long SalaId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public long UnidadeId { get; set; }
    public long? TipoSalaId { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
}
