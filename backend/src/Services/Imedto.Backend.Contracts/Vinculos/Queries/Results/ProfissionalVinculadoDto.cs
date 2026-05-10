namespace Imedto.Backend.Contracts.Vinculos.Queries.Results;

public class ProfissionalVinculadoDto
{
    public long VinculoId { get; set; }
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
}
