namespace Imedto.Backend.Contracts.Termos.Dtos;

/// <summary>
/// Payload mínimo do termo retornado no fluxo público de aceite (Fase 4).
/// Sem PII do paciente — só nome do estabelecimento + nome do profissional emissor +
/// título/conteúdo do termo.
/// </summary>
public class TermoPublicoDto
{
    public string TituloModelo { get; set; }
    public string ConteudoSnapshotHtml { get; set; }
    public string EstabelecimentoNome { get; set; }
    public string ProfissionalEmissor { get; set; }
    public DateTime EmitidoEm { get; set; }
}
