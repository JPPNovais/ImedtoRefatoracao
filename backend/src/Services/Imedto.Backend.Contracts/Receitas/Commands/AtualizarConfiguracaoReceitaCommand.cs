using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Commands;

public class AtualizarConfiguracaoReceitaCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public string? CabecalhoHtml { get; set; }
    public string? RodapeHtml { get; set; }
    public long? ModeloPadraoId { get; set; }
    public string? EmissorPadrao { get; set; }
}
