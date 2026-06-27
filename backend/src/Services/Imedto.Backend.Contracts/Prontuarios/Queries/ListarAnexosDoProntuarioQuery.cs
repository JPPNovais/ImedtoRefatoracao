using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Contracts.Prontuarios.Queries;

public class ListarAnexosDoProntuarioQuery : IQuery<PaginaAnexosDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    /// <summary>Audit LGPD: identificacao do solicitante para registrar o acesso.</summary>
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>Papel do solicitante. Dono bypassa o predicado de autoria (R4/R7 briefing 2026-06-27_001).</summary>
    public TenantPapel SolicitantePapel { get; set; }
    public long? EvolucaoId { get; set; } // opcional: só os da evolução

    /// <summary>Página 1-based. Default 1. Quando não informado, comporta-se como página 1.</summary>
    public int Pagina { get; set; } = 1;
    /// <summary>Tamanho da página. Default 50 (razoável para galeria de fotos clínicas).</summary>
    public int TamanhoPagina { get; set; } = 50;
}

/// <summary>Nova query: batch de URLs assinadas para múltiplos anexos em uma só chamada.</summary>
public class ObterUrlsAnexosQuery : IQuery<IEnumerable<AnexoUrlDto>>
{
    public IReadOnlyList<long> AnexoIds { get; set; } = Array.Empty<long>();
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    /// <summary>Papel do solicitante. Dono bypassa o predicado de autoria (R4/R7 briefing 2026-06-27_001).</summary>
    public TenantPapel SolicitantePapel { get; set; }
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
    /// <summary>Papel do solicitante. Dono bypassa o predicado de autoria (R4/R7 briefing 2026-06-27_001).</summary>
    public TenantPapel SolicitantePapel { get; set; }

    /// <summary>
    /// TTL da URL assinada em segundos. Quando 0 (default), o handler usa
    /// <c>StorageOptions.TtlSignedUrlMinutos</c> (5 min por padrão).
    /// </summary>
    public int TtlSegundos { get; set; } = 0;
}
