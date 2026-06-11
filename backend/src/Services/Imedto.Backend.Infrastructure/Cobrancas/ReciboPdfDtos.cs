namespace Imedto.Backend.Infrastructure.Cobrancas;

/// <summary>
/// DTOs internos do <see cref="QuestPdfReciboPagamentoService"/> (leitura via Dapper).
/// InternalsVisibleTo("Imedto.Backend.Test") expõe esses tipos para os testes de unidade.
/// </summary>

internal sealed record ReciboPagamentoRow(
    long PagamentoId,
    long CobrancaId,
    long PacienteId,
    string PacienteNome,
    decimal ValorPago,
    string FormaPagamentoNome,
    int Parcelas,
    DateOnly DataPagamento,
    /// <summary>Nome completo ou e-mail de quem registrou o pagamento.</summary>
    string RegistradoPorNome,
    string CobrancaOrigem,
    string? CobrancaDescricao,
    string EstabelecimentoNomeFantasia,
    string? EstabelecimentoCnpj,
    string? EstabelecimentoTelefone,
    string? EstabelecimentoEndereco,
    string? EstabelecimentoFotoUrl);

internal sealed record DadosReciboPdf(ReciboPagamentoRow Dados)
{
    /// <summary>Bytes da logo do estabelecimento (null = usa placeholder de iniciais).</summary>
    public byte[] LogoBytes { get; init; }
}
