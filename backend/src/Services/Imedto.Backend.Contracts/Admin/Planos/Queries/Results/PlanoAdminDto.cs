namespace Imedto.Backend.Contracts.Admin.Planos.Queries.Results;

public class PlanoAdminDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? DescricaoCurta { get; set; }
    public int? PrecoMensalCentavos { get; set; }
    public bool Gratuito { get; set; }
    public bool Ativo { get; set; }
    public string LimitesJson { get; set; } = "{}";
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? AtualizadoEm { get; set; }
}

public record ListarPlanosAdminResult(
    IReadOnlyList<PlanoAdminDto> Itens,
    int Total,
    int Pagina,
    int Tamanho);
