using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Cadastros.Commands;

public class CriarFabricanteEstoqueCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Pais { get; set; }
    public long FabricanteIdCriado { get; set; }
}

public class AtualizarFabricanteEstoqueCommand : ICommand
{
    public long FabricanteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Pais { get; set; }
}

public class InativarFabricanteEstoqueCommand : ICommand
{
    public long FabricanteId { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class ReativarFabricanteEstoqueCommand : ICommand
{
    public long FabricanteId { get; set; }
    public long EstabelecimentoId { get; set; }
}
