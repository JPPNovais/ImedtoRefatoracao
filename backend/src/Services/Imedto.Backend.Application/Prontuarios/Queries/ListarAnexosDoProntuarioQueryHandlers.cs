using Microsoft.Extensions.Options;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.Infrastructure.Storage;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Queries;

public class ListarAnexosDoProntuarioQueryHandlers
    : IRequestHandler<ListarAnexosDoProntuarioQuery, IEnumerable<AnexoDto>>
{
    private readonly ProntuarioAnexoQueryRepository _queryRepository;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ListarAnexosDoProntuarioQueryHandlers(
        ProntuarioAnexoQueryRepository queryRepository,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepository = queryRepository;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
    }

    public async Task<IEnumerable<AnexoDto>> Handle(ListarAnexosDoProntuarioQuery query)
    {
        var prontuario = await _prontuarioRepo.ObterPorPaciente(query.PacienteId, query.EstabelecimentoId);
        if (prontuario is null) return Array.Empty<AnexoDto>();

        // Audit LGPD: nomes de anexo podem indicar diagnostico ("Mamografia 2024.pdf").
        // Auditar mesmo se a lista vier vazia — saber que houve consulta eh informacao relevante.
        await _acessoLog.RegistrarAsync(
            prontuario.Id, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);

        return await _queryRepository.ListarDoProntuario(prontuario.Id, query.EvolucaoId);
    }
}

public class ObterUrlAnexoQueryHandlers : IRequestHandler<ObterUrlAnexoQuery, AnexoUrlDto>
{
    private readonly ProntuarioAnexoQueryRepository _queryRepository;
    private readonly IAnexoStorageService _storage;
    private readonly IProntuarioAcessoLogService _acessoLog;
    private readonly StorageOptions _storageOptions;

    public ObterUrlAnexoQueryHandlers(
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

    public async Task<AnexoUrlDto> Handle(ObterUrlAnexoQuery query)
    {
        // Defense-in-depth LGPD: o repositorio filtra por tenant E por paciente.
        // Anexo de outro tenant ou de outro paciente do mesmo tenant retorna null —
        // mesma mensagem generica em todos os casos para nao vazar existencia.
        var referencia = await _queryRepository.ObterReferenciaAnexo(
            query.AnexoId, query.PacienteId, query.EstabelecimentoId);
        if (referencia is null)
            throw new BusinessException("Anexo não encontrado.");

        var (prontuarioId, storagePath, nome, mime) = referencia.Value;

        // TTL vem de StorageOptions (default 5 min). Mantém compatibilidade caso o caller
        // queira sobrescrever via TtlSegundos > 0 (ex.: testes ou caso de uso pontual).
        var ttlSegundos = query.TtlSegundos > 0
            ? query.TtlSegundos
            : _storageOptions.TtlSignedUrlMinutos * 60;

        var url = await _storage.GerarUrlAssinadaLeituraAsync(storagePath, ttlSegundos);

        // Audit LGPD: cada emissão de URL assinada para anexo de prontuário é acesso de leitura.
        // Registrado APÓS gerar a URL para não logar tentativas que falharam no storage.
        await _acessoLog.RegistrarAsync(prontuarioId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);

        return new AnexoUrlDto
        {
            Id = query.AnexoId,
            NomeOriginal = nome,
            MimeType = mime,
            Url = url,
            ExpiraEm = DateTime.UtcNow.AddSeconds(ttlSegundos)
        };
    }
}
