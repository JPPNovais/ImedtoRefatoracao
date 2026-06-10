using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Cobrancas.Commands;

/// <summary>Cria ou atualiza preço de consulta para um profissional (ou padrão do estabelecimento).</summary>
public class SalvarTabelaPrecoConsultaCommand : ICommand
{
    /// <summary>null = criar novo; valor > 0 = atualizar existente.</summary>
    public long? Id { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>null = preço padrão do estabelecimento.</summary>
    public Guid? ProfissionalId { get; set; }
    public decimal ValorSugerido { get; set; }
}
