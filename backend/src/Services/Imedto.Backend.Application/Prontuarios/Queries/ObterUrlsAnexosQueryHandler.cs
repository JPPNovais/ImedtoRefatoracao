using Microsoft.Extensions.Options;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure.Storage;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Queries;

/// <summary>
/// Gera URLs assinadas para múltiplos anexos em uma única chamada (evita N+1 do mobile).
///
/// Scoped — depende de IProntuarioAcessoLogService (scoped).
/// Defense-in-depth: a query de storage filtra por (paciente, estabelecimento) — anexoIds
/// de outro paciente ou tenant simplesmente não retornam resultados (sem leak de existência).
/// Um único acesso de auditoria é registrado por prontuário acessado no batch.
/// </summary>
public class ObterUrlsAnexosQueryHandler
    : IRequestHandler<ObterUrlsAnexosQuery, IEnumerable<AnexoUrlDto>>
{
    private readonly ProntuarioAnexoQueryRepository _queryRepository;
    private readonly IAnexoStorageService _storage;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly StorageOptions _storageOptions;

    public ObterUrlsAnexosQueryHandler(
        ProntuarioAnexoQueryRepository queryRepository,
        IAnexoStorageService storage,
        IProntuarioAcessoLogService acessoLog,
        IOptions<StorageOptions> storageOptions)
    {
        _queryRepository = queryRepository;
        _storage = storage;
        _acessoLog = acessoLog;
        _storageOptions = storageOptions.Value;
    }

    public async Task<IEnumerable<AnexoUrlDto>> Handle(ObterUrlsAnexosQuery query)
    {
        if (query.AnexoIds.Count == 0) return Enumerable.Empty<AnexoUrlDto>();

        // Limite de segurança: não aceita batches gigantes que possam sobrecarregar o S3.
        var ids = query.AnexoIds.Take(50).ToList();

        var referencias = await _queryRepository.ObterReferenciasAnexos(
            ids, query.PacienteId, query.EstabelecimentoId);

        var referenciasList = referencias.ToList();
        if (referenciasList.Count == 0) return Enumerable.Empty<AnexoUrlDto>();

        var ttlSegundos = _storageOptions.TtlSignedUrlMinutos * 60;
        var expiraEm = DateTime.UtcNow.AddSeconds(ttlSegundos);

        var resultado = new List<AnexoUrlDto>(referenciasList.Count);

        // URLs geradas sequencialmente para não violar regra Npgsql (uma query por conn).
        // O gargalo aqui é o S3 (cada URL é gerada localmente via SDK sem IO real), não o banco.
        foreach (var (anexoId, prontuarioId, storagePath, nome, mime) in referenciasList)
        {
            var url = await _storage.GerarUrlAssinadaLeituraAsync(storagePath, ttlSegundos);
            resultado.Add(new AnexoUrlDto
            {
                Id = anexoId,
                NomeOriginal = nome,
                MimeType = mime,
                Url = url,
                ExpiraEm = expiraEm
            });
        }

        // Audit LGPD: um log por prontuário distinto acessado no batch.
        var prontuariosDistintos = referenciasList
            .Select(r => r.ProntuarioId)
            .Distinct();

        foreach (var prontuarioId in prontuariosDistintos)
        {
            await _acessoLog.RegistrarAsync(
                prontuarioId,
                query.SolicitanteUsuarioId,
                query.EstabelecimentoId,
                TipoAcessoProntuario.Leitura);
        }

        return resultado;
    }
}
