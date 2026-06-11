namespace Imedto.Backend.Contracts.Cobrancas.Queries.Results;

/// <summary>
/// DTO raiz da aba Financeiro do paciente (F2 — CA23/DC7).
/// KPIs calculados no backend (R3); lista de cobranças com pagamentos e estornos.
/// Minimização LGPD: sem dado clínico, sem PII além do necessário para a tela.
/// </summary>
public class FinanceiroAbaDto
{
    // ── KPIs (DC7 / R3) ──────────────────────────────────────────────────────
    public decimal TotalCobrado { get; set; }
    public decimal TotalPagoLiquido { get; set; }
    public decimal Saldo { get; set; }

    public IEnumerable<CobrancaAbaDto> Cobrancas { get; set; } = Array.Empty<CobrancaAbaDto>();
}

/// <summary>Cobrança na lista da aba. Um card por cobrança (ChargeCard do protótipo).</summary>
public class CobrancaAbaDto
{
    public long Id { get; set; }
    public string Origem { get; set; } = string.Empty;
    public string TipoAtendimento { get; set; } = string.Empty;
    // F6: convênio associado (null para Particular)
    public long? ConvenioId { get; set; }
    public string? ConvenioNome { get; set; }
    public decimal ValorCobrado { get; set; }
    public decimal Desconto { get; set; }
    public decimal TotalLiquido { get; set; }
    public decimal TotalPagoLiquido { get; set; }
    public decimal Saldo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    // F6/R10: guia de autorização — null = guia pendente
    public string? GuiaNumero { get; set; }
    public string? GuiaSenha { get; set; }
    public DateOnly? GuiaAutorizadaEm { get; set; }

    // Expansão: pagamentos, estornos, histórico de valor (cirurgia — F5 popula; F2 exibe se existir)
    public IEnumerable<PagamentoAbaDto> Pagamentos { get; set; } = Array.Empty<PagamentoAbaDto>();
    public IEnumerable<HistoricoValorAbaDto> HistoricoValor { get; set; } = Array.Empty<HistoricoValorAbaDto>();
}

/// <summary>Pagamento exibido na expansão do card. Inclui flag se foi estornado (R8/CA26).</summary>
public class PagamentoAbaDto
{
    public long Id { get; set; }
    public decimal Valor { get; set; }
    public string FormaPagamentoNome { get; set; } = string.Empty;
    public int Parcelas { get; set; }
    public decimal Taxa { get; set; }
    public DateOnly DataPagamento { get; set; }
    public bool Estornado { get; set; }
    public EstornoAbaDto? Estorno { get; set; }
}

/// <summary>Dados do estorno exibido em destaque vermelho na expansão do card (CA26).</summary>
public class EstornoAbaDto
{
    public long Id { get; set; }
    public decimal Valor { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string EstornadoPorNome { get; set; } = string.Empty;
    public DateOnly DataEstorno { get; set; }
}

/// <summary>
/// Linha de histórico de alteração de valor da cirurgia (F5 popula; F2 exibe read-only — DC4).
/// Incluída no DTO mas vazia na F2 (lista sempre vazia até F5). CA27.
/// </summary>
public class HistoricoValorAbaDto
{
    public decimal ValorAnterior { get; set; }
    public decimal ValorNovo { get; set; }
    public string AlteradoPorNome { get; set; } = string.Empty;
    public DateTime AlteradoEm { get; set; }
}
