using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Commands;

public class ReabrirCaixaDiarioCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public DateOnly Data { get; set; }
    /// <summary>UsuarioId do Dono (verificação de papel no handler — CA167).</summary>
    public Guid UsuarioId { get; set; }
    public bool EhDono { get; set; }
}
