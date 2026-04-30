using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Orcamentos.Catalogos.Commands;

// ──────────── Cirurgias ────────────

public class CriarCatalogoCirurgiaCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorBase { get; set; }
    public int? DuracaoPadraoMinutos { get; set; }
    public long IdCriado { get; set; }
}

public class AtualizarCatalogoCirurgiaCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorBase { get; set; }
    public int? DuracaoPadraoMinutos { get; set; }
}

public class RemoverCatalogoCirurgiaCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

// ──────────── Valor profissional ────────────

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

// ──────────── Configuração local cirurgia (1:1 por tipo) ────────────

public class SalvarConfiguracaoLocalCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string TipoInternacao { get; set; } = string.Empty;
    public int TempoBaseMinutos { get; set; }
    public decimal ValorBase { get; set; }
    public int TempoAdicionalMinutos { get; set; }
    public decimal ValorAdicional { get; set; }
    public long IdSalvo { get; set; }
}

// ──────────── Equipes ────────────

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

// ──────────── Implantes ────────────

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

// ──────────── Configuração pagamento ────────────

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

// ──────────── Produtos ────────────

public class CriarCatalogoProdutoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal? ValorReferencia { get; set; }
    public bool UsoUnico { get; set; }
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
}

public class RemoverCatalogoProdutoCommand : ICommand
{
    public long Id { get; set; }
    public long EstabelecimentoId { get; set; }
}

// ──────────── Vínculo cirurgia × produto ────────────

public class VincularProdutoCirurgiaCommand : ICommand
{
    public long CatalogoCirurgiaId { get; set; }
    public long CatalogoProdutoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public decimal QuantidadePadrao { get; set; } = 1m;
    public bool Obrigatorio { get; set; }
    public long IdCriado { get; set; }
}

public class AtualizarVinculoProdutoCirurgiaCommand : ICommand
{
    public long VinculoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public decimal QuantidadePadrao { get; set; }
    public bool Obrigatorio { get; set; }
}

public class DesvincularProdutoCirurgiaCommand : ICommand
{
    public long VinculoId { get; set; }
    public long EstabelecimentoId { get; set; }
}
