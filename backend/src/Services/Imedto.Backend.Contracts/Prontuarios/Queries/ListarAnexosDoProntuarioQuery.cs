using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

public class ListarAnexosDoProntuarioQuery : IQuery<IEnumerable<AnexoDto>>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Audit LGPD: identificacao do solicitante para registrar o acesso.</summary>
    public Guid SolicitanteUsuarioId { get; set; }
    public long? EvolucaoId { get; set; } // opcional: só os da evolução
}

public class ObterUrlAnexoQuery : IQuery<AnexoUrlDto>
{
    public long AnexoId { get; set; }
    /// <summary>
    /// Paciente da URL pedida — defense-in-depth LGPD: o filtro garante
    /// que o anexo só seja servido se realmente pertencer a este paciente.
    /// Sem este filtro, qualquer membro do tenant baixava anexo trocando
    /// somente o <c>anexoId</c> na URL.
    /// </summary>
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }

    /// <summary>
    /// TTL da URL assinada em segundos. Quando 0 (default), o handler usa
    /// <c>StorageOptions.TtlSignedUrlMinutos</c> (5 min por padrão).
    /// </summary>
    public int TtlSegundos { get; set; } = 0;
}
