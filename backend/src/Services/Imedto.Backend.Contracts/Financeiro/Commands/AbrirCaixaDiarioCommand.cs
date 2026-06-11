using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class AbrirCaixaDiarioCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public DateOnly Data { get; set; }
    public Guid UsuarioId { get; set; }
}
