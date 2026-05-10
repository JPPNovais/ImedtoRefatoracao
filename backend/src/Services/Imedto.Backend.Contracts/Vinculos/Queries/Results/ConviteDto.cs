namespace Imedto.Backend.Contracts.Vinculos.Queries.Results;

public class ConviteDto
{
    public long VinculoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string NomeFantasiaEstabelecimento { get; set; }
    // ConvidadoPorEmail removido (LGPD): front nao exibe (so estava na interface TS,
    // sem uso real em template). Front mostra ConvidadoPorNome.
    public string ConvidadoPorNome { get; set; }
    public DateTime ConvidadoEm { get; set; }

    /// <summary>Dados pré-cadastrados pelo convidador para o onboarding (todos opcionais).</summary>
    public string NomeConvidado { get; set; }
    public string TelefoneConvidado { get; set; }
    public string EspecialidadeConvidada { get; set; }
    public long? ProfissaoConvidadaId { get; set; }
    public string ProfissaoConvidadaNome { get; set; }
    public long? ModeloPermissaoId { get; set; }
}
