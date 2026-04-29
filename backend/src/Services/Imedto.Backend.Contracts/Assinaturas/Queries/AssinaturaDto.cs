namespace Imedto.Backend.Contracts.Assinaturas.Queries;

/// <summary>
/// Resposta do endpoint "GET /api/minha-assinatura". Embute o plano vigente e o cálculo
/// de dias restantes do trial — preferimos resolver no backend para não duplicar lógica
/// de fuso horário e arredondamento no frontend.
/// </summary>
public class AssinaturaDto
{
    public PlanoDto Plano { get; set; } = new();

    /// <summary>String do enum <c>StatusAssinatura</c>: Trial | Ativa | Suspensa | Cancelada | Expirada.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime IniciadaEm { get; set; }
    public DateTime? ExpiraEm { get; set; }

    /// <summary>
    /// Dias completos restantes até <see cref="ExpiraEm"/>. null se não há expiração ou se
    /// já passou. Apenas calculado para Trial — Ativa pode usar este campo se houver janela
    /// de renovação (não é o caso atual).
    /// </summary>
    public int? DiasRestantes { get; set; }
}
