using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Commands;

public class InativarTabelaPrecoConsultaCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}
