using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Cadastros.Commands;

public class CriarCategoriaEstoqueCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cor { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    /// <summary>Populado pelo handler após o INSERT.</summary>
    public long CategoriaIdCriada { get; set; }
}

public class AtualizarCategoriaEstoqueCommand : ICommand
{
    public long CategoriaId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cor { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
}

public class InativarCategoriaEstoqueCommand : ICommand
{
    public long CategoriaId { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class ReativarCategoriaEstoqueCommand : ICommand
{
    public long CategoriaId { get; set; }
    public long EstabelecimentoId { get; set; }
}
