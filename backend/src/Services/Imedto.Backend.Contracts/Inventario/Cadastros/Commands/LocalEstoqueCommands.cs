using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Cadastros.Commands;

public class CriarLocalEstoqueCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    /// <summary>Nome do enum TipoLocalEstoque (Armario, Gaveta, ...). Handler valida.</summary>
    public string Tipo { get; set; } = string.Empty;
    public string? AndarSetor { get; set; }
    public string? Responsavel { get; set; }
    public long LocalIdCriado { get; set; }
}

public class AtualizarLocalEstoqueCommand : ICommand
{
    public long LocalId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? AndarSetor { get; set; }
    public string? Responsavel { get; set; }
}

public class InativarLocalEstoqueCommand : ICommand
{
    public long LocalId { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class ReativarLocalEstoqueCommand : ICommand
{
    public long LocalId { get; set; }
    public long EstabelecimentoId { get; set; }
}
