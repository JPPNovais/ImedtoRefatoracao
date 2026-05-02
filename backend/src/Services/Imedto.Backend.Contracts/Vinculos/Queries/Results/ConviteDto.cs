namespace Imedto.Backend.Contracts.Vinculos.Queries.Results;

public class ConviteDto
{
    public long VinculoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string NomeFantasiaEstabelecimento { get; set; }
    public string ConvidadoPorEmail { get; set; }
    public string ConvidadoPorNome { get; set; }
    public DateTime ConvidadoEm { get; set; }

    /// <summary>Dados pré-cadastrados pelo convidador para o onboarding (todos opcionais).</summary>
    public string NomeConvidado { get; set; }
    public string TelefoneConvidado { get; set; }
    public string EspecialidadeConvidada { get; set; }
    public long? ModeloPermissaoId { get; set; }
}
