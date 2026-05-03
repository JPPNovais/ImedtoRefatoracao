namespace Imedto.Backend.Contracts.Vinculos.Queries.Results;

/// <summary>
/// View de solicitação de vínculo. Usado tanto na lista do profissional ("minhas")
/// quanto na do estabelecimento ("recebidas") — campos identificadores variam de relevância,
/// mas o shape comum simplifica o frontend.
/// </summary>
public class SolicitacaoVinculoDto
{
    public long Id { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    // ProfissionalEmail removido (LGPD): vazamento PII — front nao exibe nem usa
    // (cross-check 0 matches em frontend/src). Auditoria Fase 1 sinalizou como alto risco.
    public string ProfissionalNome { get; set; }
    public long EstabelecimentoId { get; set; }
    // EstabelecimentoNomeFantasia removido: front nao consome (0 matches).
    public string Status { get; set; }
    public string Mensagem { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? RespondidaEm { get; set; }
    public string MotivoRecusa { get; set; }
}
