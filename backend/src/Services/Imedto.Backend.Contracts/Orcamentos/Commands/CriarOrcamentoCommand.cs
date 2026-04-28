using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Commands;

public class CriarOrcamentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public long PacienteId { get; set; }
    public DateOnly Validade { get; set; }
    public string? Observacoes { get; set; }
    public Guid CriadoPorUsuarioId { get; set; }
    public List<ItemOrcamentoPayload> Itens { get; set; } = new();

    public long OrcamentoIdCriado { get; set; }
}

public record ItemOrcamentoPayload(string Descricao, decimal Quantidade, decimal ValorUnitario, decimal DescontoPercent);
