namespace Imedto.Backend.Contracts.Orcamentos.Queries.Results;

public class PreviewOrcamentoDto
{
    public decimal TotalCirurgias { get; set; }
    public decimal TotalEquipe { get; set; }
    public decimal TotalImplantes { get; set; }
    /// <summary>Valor calculado do local cirúrgico (substitui o antigo TotalInternacao).</summary>
    public decimal TotalLocal { get; set; }
    public decimal TotalAnestesia { get; set; }
    public decimal TotalItens { get; set; }

    /// <summary>Soma de todas as collections (sem desconto/acréscimo).</summary>
    public decimal TotalGeral { get; set; }

    /// <summary>Soma dos valores das formas de pagamento (sem acréscimo).</summary>
    public decimal SomaFormas { get; set; }

    /// <summary>TotalGeral - SomaFormas. Zero quando integro.</summary>
    public decimal Diferenca { get; set; }

    /// <summary>True quando |Diferença| ≤ R$ 0,01 ou não há formas informadas.</summary>
    public bool IntegridadeOk { get; set; }

    public List<FormaPagamentoCalculadaDto> Formas { get; set; } = new();

    /// <summary>
    /// Honorários calculados por membro de equipe quando informado <c>valorProfissionalId</c>
    /// em <c>EquipeComCatalogo</c>. Vazio se não foi solicitado.
    /// </summary>
    public List<EquipeCalculadaDto> Equipes { get; set; } = new();
}

/// <summary>Resultado por membro: id do catálogo + valor calculado para o tempo informado.</summary>
public class EquipeCalculadaDto
{
    public long ValorProfissionalId { get; set; }
    public int TempoMinutos { get; set; }
    public int Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
}

public class FormaPagamentoCalculadaDto
{
    public long FormaPagamentoId { get; set; }
    public string? FormaPagamentoNome { get; set; }
    public decimal Valor { get; set; }
    public int Parcelas { get; set; }
    public decimal AcrescimoPercentual { get; set; }
    public decimal EntradaPercentual { get; set; }

    /// <summary>Valor + acréscimo aplicado.</summary>
    public decimal TotalBruto { get; set; }
    public decimal Entrada { get; set; }
    public decimal ValorParcela { get; set; }
}
