using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Catalogos.Commands;

public class CriarCatalogoCirurgiaCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorBase { get; set; }
    public int? DuracaoPadraoMinutos { get; set; }
    public string? CodigoInterno { get; set; }
    public string? CodigoTuss { get; set; }
    public string? Categoria { get; set; }
    public long IdCriado { get; set; }
}

public class AtualizarCatalogoCirurgiaCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorBase { get; set; }
    public int? DuracaoPadraoMinutos { get; set; }
    public string? CodigoInterno { get; set; }
    public string? CodigoTuss { get; set; }
    public string? Categoria { get; set; }
}

public class RemoverCatalogoCirurgiaCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class CriarValorProfissionalCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid? ProfissionalUsuarioId { get; set; }
    public string Funcao { get; set; } = string.Empty;
    public int TempoBaseMinutos { get; set; }
    public decimal ValorTempoBase { get; set; }
    public int TempoAdicionalMinutos { get; set; }
    public decimal ValorAdicional { get; set; }
    public decimal ValorPlus { get; set; }
    public long IdCriado { get; set; }
}

public class AtualizarValorProfissionalCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Funcao { get; set; } = string.Empty;
    public int TempoBaseMinutos { get; set; }
    public decimal ValorTempoBase { get; set; }
    public int TempoAdicionalMinutos { get; set; }
    public decimal ValorAdicional { get; set; }
    public decimal ValorPlus { get; set; }
}

public class RemoverValorProfissionalCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class SalvarConfiguracaoLocalCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    /// <summary>Tipo do local cirúrgico (5 valores do enum <c>TipoLocalCirurgia</c>).</summary>
    public string TipoLocal { get; set; } = string.Empty;
    public int TempoBaseMinutos { get; set; }
    public decimal ValorBase { get; set; }
    public int TempoAdicionalMinutos { get; set; }
    public decimal ValorAdicional { get; set; }
    public long IdSalvo { get; set; }
}

public class CriarCatalogoEquipeCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorPadrao { get; set; }
    public long IdCriado { get; set; }
}

public class AtualizarCatalogoEquipeCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorPadrao { get; set; }
}

public class RemoverCatalogoEquipeCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class CriarCatalogoImplanteCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public long? ItemInventarioId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal CustoUnitario { get; set; }
    public long IdCriado { get; set; }
}

public class AtualizarCatalogoImplanteCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public long? ItemInventarioId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal CustoUnitario { get; set; }
}

public class RemoverCatalogoImplanteCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class CriarConfiguracaoPagamentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public long FormaPagamentoId { get; set; }
    public decimal AcrescimoPercentual { get; set; }
    public decimal EntradaPercentualPadrao { get; set; }
    public decimal TaxaParcela { get; set; }
    public int ParcelasMaximas { get; set; }
    public long IdCriado { get; set; }
}

public class AtualizarConfiguracaoPagamentoCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public decimal AcrescimoPercentual { get; set; }
    public decimal EntradaPercentualPadrao { get; set; }
    public decimal TaxaParcela { get; set; }
    public int ParcelasMaximas { get; set; }
}

public class RemoverConfiguracaoPagamentoCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class CriarCatalogoProdutoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal? ValorReferencia { get; set; }
    public bool UsoUnico { get; set; }
    public string? Tipo { get; set; }
    public string? Marca { get; set; }
    public string? Unidade { get; set; }
    public string? FornecedorNome { get; set; }
    public string? CodigoSku { get; set; }
    public long IdCriado { get; set; }
}

public class AtualizarCatalogoProdutoCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal? ValorReferencia { get; set; }
    public bool UsoUnico { get; set; }
    public string? Tipo { get; set; }
    public string? Marca { get; set; }
    public string? Unidade { get; set; }
    public string? FornecedorNome { get; set; }
    public string? CodigoSku { get; set; }
}

public class RemoverCatalogoProdutoCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class VincularProdutoCirurgiaCommand : ICommand
{
    public long CatalogoCirurgiaId { get; set; }
    public long CatalogoProdutoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public decimal QuantidadePadrao { get; set; } = 1m;
    public bool Obrigatorio { get; set; }
    public bool Incluido { get; set; } = true;
    public long IdCriado { get; set; }
}

public class AtualizarVinculoProdutoCirurgiaCommand : ICommand
{
    public long VinculoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public decimal QuantidadePadrao { get; set; }
    public bool Obrigatorio { get; set; }
    public bool Incluido { get; set; } = true;
}

public class DesvincularProdutoCirurgiaCommand : ICommand
{
    public long VinculoId { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class CriarOrcamentoTeamRoleCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Papel { get; set; } = string.Empty;
    public Guid? ProfissionalUsuarioId { get; set; }
    public string? NomePadrao { get; set; }
    public string TipoHonorario { get; set; } = "Percentual";
    public decimal Valor { get; set; }
    public string BaseCalculo { get; set; } = "procedimento";
    public long IdCriado { get; set; }
}

public class AtualizarOrcamentoTeamRoleCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Papel { get; set; } = string.Empty;
    public Guid? ProfissionalUsuarioId { get; set; }
    public string? NomePadrao { get; set; }
    public string TipoHonorario { get; set; } = "Percentual";
    public decimal Valor { get; set; }
    public string BaseCalculo { get; set; } = "procedimento";
}

public class RemoverOrcamentoTeamRoleCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class FaixaAnestesistaInput
{
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public class CriarOrcamentoAnestesistaCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? ProfissionalUsuarioId { get; set; }
    public string? Crm { get; set; }
    public string? Especialidade { get; set; }
    public string? Telefone { get; set; }
    public string? TabelaHonorarios { get; set; }
    public List<FaixaAnestesistaInput> Faixas { get; set; } = new();
    public long IdCriado { get; set; }
}

public class AtualizarOrcamentoAnestesistaCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid? ProfissionalUsuarioId { get; set; }
    public string? Crm { get; set; }
    public string? Especialidade { get; set; }
    public string? Telefone { get; set; }
    public string? TabelaHonorarios { get; set; }
    public List<FaixaAnestesistaInput> Faixas { get; set; } = new();
}

public class RemoverOrcamentoAnestesistaCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

public class ProdutoDoPacoteInput
{
    public long ProdutoId { get; set; }
    public decimal Quantidade { get; set; } = 1m;
}

public class CriarOrcamentoPacoteCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public long? AnestesistaId { get; set; }
    public decimal? ValorTotalSugerido { get; set; }
    public List<long> ProcedimentoIds { get; set; } = new();
    public List<ProdutoDoPacoteInput> Produtos { get; set; } = new();
    public List<long> TeamRoleIds { get; set; } = new();
    public long IdCriado { get; set; }
}

public class AtualizarOrcamentoPacoteCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public long? AnestesistaId { get; set; }
    public decimal? ValorTotalSugerido { get; set; }
    public List<long> ProcedimentoIds { get; set; } = new();
    public List<ProdutoDoPacoteInput> Produtos { get; set; } = new();
    public List<long> TeamRoleIds { get; set; } = new();
}

public class RemoverOrcamentoPacoteCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}
