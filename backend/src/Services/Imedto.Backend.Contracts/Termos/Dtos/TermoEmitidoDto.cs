namespace Imedto.Backend.Contracts.Termos.Dtos;

/// <summary>Resumo do termo emitido (listagem do paciente).</summary>
public class TermoEmitidoResumoDto
{
    public long Id { get; set; }
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public long TermoModeloId { get; set; }
    public string TermoModeloTitulo { get; set; }
    public string Categoria { get; set; }
    public int VersaoModelo { get; set; }
    public string Status { get; set; }
    public string AssinaturaTipo { get; set; }
    public DateTime? AssinadoEm { get; set; }
    public DateTime? TokenExpiraEm { get; set; }
    public bool TemPdf { get; set; }
    public DateTime CriadoEm { get; set; }
    public Guid EmitidoPorUsuarioId { get; set; }
    public string EmitidoPorNome { get; set; }
}

/// <summary>Detalhe completo (inclui snapshot HTML e dados de revogação).</summary>
public class TermoEmitidoDetalheDto : TermoEmitidoResumoDto
{
    public string ConteudoSnapshotHtml { get; set; }
    public string ConteudoSnapshotTexto { get; set; }
    public string HashIntegridade { get; set; }
    public string IpAssinatura { get; set; }
    public string UserAgentAssinatura { get; set; }
    public DateTime? RevogadoEm { get; set; }
    public string RevogadoMotivo { get; set; }
}

public class TermoPdfUrlDto
{
    public string Url { get; set; }
    public int TtlSegundos { get; set; }
}
