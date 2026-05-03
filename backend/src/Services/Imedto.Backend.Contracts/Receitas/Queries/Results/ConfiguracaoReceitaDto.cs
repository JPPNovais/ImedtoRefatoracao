namespace Imedto.Backend.Contracts.Receitas.Queries.Results;

public class ConfiguracaoReceitaDto
{
    public long EstabelecimentoId { get; set; }
    public string? CabecalhoHtml { get; set; }
    public string? RodapeHtml { get; set; }
    public long? ModeloPadraoId { get; set; }
    public string? EmissorPadrao { get; set; }
    public DateTime? AtualizadaEm { get; set; }
}

// MedicamentoFavoritoDto removido — endpoint GET /api/receitas/medicamentos-favoritos
// nao tinha consumidor no front (decisao Fase 1 do plano). A entidade de dominio
// MedicamentoFavorito + IMedicamentoFavoritoRepository continuam — sao usados em
// EmitirReceitaCommandHandler para registrar uso de medicamentos.
