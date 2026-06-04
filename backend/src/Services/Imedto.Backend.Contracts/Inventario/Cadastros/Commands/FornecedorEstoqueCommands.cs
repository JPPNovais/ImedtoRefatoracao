using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Inventario.Cadastros.Commands;

public class CriarFornecedorEstoqueCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? Cnpj { get; set; }
    public string? ContatoNome { get; set; }
    public string? ContatoTelefone { get; set; }
    public string? ContatoEmail { get; set; }
    public int PrazoEntregaDias { get; set; } = 5;
    public string TipoPrazoEntrega { get; set; } = "corridos";
    public long FornecedorIdCriado { get; set; }
}

public class AtualizarFornecedorEstoqueCommand : ICommand
{
    public long FornecedorId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? Cnpj { get; set; }
    public string? ContatoNome { get; set; }
    public string? ContatoTelefone { get; set; }
    public string? ContatoEmail { get; set; }
    public int PrazoEntregaDias { get; set; }
    public string TipoPrazoEntrega { get; set; } = "corridos";
}

public class InativarFornecedorEstoqueCommand : ICommand
{
    public long FornecedorId { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class ReativarFornecedorEstoqueCommand : ICommand
{
    public long FornecedorId { get; set; }
    public long EstabelecimentoId { get; set; }
}
