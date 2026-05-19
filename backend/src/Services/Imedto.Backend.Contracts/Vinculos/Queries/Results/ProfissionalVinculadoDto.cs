namespace Imedto.Backend.Contracts.Vinculos.Queries.Results;

public class ProfissionalVinculadoDto
{
    /// <summary>
    /// Id do vínculo profissional-estabelecimento. <c>null</c> quando a linha é
    /// sintética para o próprio Dono — o dono não tem vínculo formal, mas aparece
    /// na listagem por consistência da UI. O front deve identificá-lo via
    /// <see cref="Status"/> = <c>"Dono"</c>.
    /// </summary>
    public long? VinculoId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Email { get; set; }
    public string NomeCompleto { get; set; }
    public string Status { get; set; }
    public long? ModeloPermissaoId { get; set; }
    public string ModeloPermissaoNome { get; set; }
    public DateTime ConvidadoEm { get; set; }
    public DateTime? AceitoEm { get; set; }
    public string? Especialidade { get; set; }
    public string? Conselho { get; set; }

    /// <summary>
    /// Nome da profissão. Hoje vem apenas de convites (via profissao_convidada_id);
    /// profissionais já ativados não persistem o vínculo com o catálogo de profissões.
    /// </summary>
    public string? Profissao { get; set; }

    /// <summary>URL presigned (S3) da foto do profissional, quando houver.</summary>
    public string? FotoUrl { get; set; }
}
