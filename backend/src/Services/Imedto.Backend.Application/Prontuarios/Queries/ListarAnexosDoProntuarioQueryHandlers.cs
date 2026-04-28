using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Queries;

public class ListarAnexosDoProntuarioQueryHandlers
    : IRequestHandler<ListarAnexosDoProntuarioQuery, IEnumerable<AnexoDto>>
{
    private readonly ProntuarioAnexoQueryRepository _queryRepository;
    private readonly IProntuarioRepository _prontuarioRepo;

    public ListarAnexosDoProntuarioQueryHandlers(
        ProntuarioAnexoQueryRepository queryRepository,
        IProntuarioRepository prontuarioRepo)
    {
        _queryRepository = queryRepository;
        _prontuarioRepo = prontuarioRepo;
    }

    public async Task<IEnumerable<AnexoDto>> Handle(ListarAnexosDoProntuarioQuery query)
    {
        var prontuario = await _prontuarioRepo.ObterPorPaciente(query.PacienteId, query.EstabelecimentoId);
        if (prontuario is null) return Array.Empty<AnexoDto>();

        return await _queryRepository.ListarDoProntuario(prontuario.Id, query.EvolucaoId);
    }
}

public class ObterUrlAnexoQueryHandlers : IRequestHandler<ObterUrlAnexoQuery, AnexoUrlDto>
{
    private readonly ProntuarioAnexoQueryRepository _queryRepository;
    private readonly IAnexoStorageService _storage;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ObterUrlAnexoQueryHandlers(
        ProntuarioAnexoQueryRepository queryRepository,
        IAnexoStorageService storage,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepository = queryRepository;
        _storage = storage;
        _acessoLog = acessoLog;
    }

    public async Task<AnexoUrlDto> Handle(ObterUrlAnexoQuery query)
    {
        var referencia = await _queryRepository.ObterReferenciaAnexo(query.AnexoId);
        if (referencia is null)
            throw new BusinessException("Anexo não encontrado.");

        var (prontuarioId, estabelecimentoId, storagePath, nome, mime) = referencia.Value;

        if (estabelecimentoId != query.EstabelecimentoId)
            throw new BusinessException("Anexo não pertence a este estabelecimento.");

        var url = await _storage.GerarUrlAssinadaLeituraAsync(storagePath, query.TtlSegundos);

        await _acessoLog.RegistrarAsync(prontuarioId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);

        return new AnexoUrlDto
        {
            Id = query.AnexoId,
            NomeOriginal = nome,
            MimeType = mime,
            Url = url,
            ExpiraEm = DateTime.UtcNow.AddSeconds(query.TtlSegundos)
        };
    }
}
