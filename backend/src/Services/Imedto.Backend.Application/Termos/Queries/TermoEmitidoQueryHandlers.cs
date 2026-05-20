using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.Contracts.Termos.Queries;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Queries;

public sealed class ListarTermosDoPacienteQueryHandlers
    : IRequestHandler<ListarTermosDoPacienteQuery, IReadOnlyList<TermoEmitidoResumoDto>>
{
    private readonly ITermoEmitidoQueryRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly ITermoAuditLogger _audit;

    public ListarTermosDoPacienteQueryHandlers(
        ITermoEmitidoQueryRepository repo,
        IPacienteRepository pacienteRepo,
        ITermoAuditLogger audit)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
        _audit = audit;
    }

    public async Task<IReadOnlyList<TermoEmitidoResumoDto>> Handle(ListarTermosDoPacienteQuery q)
    {
        // Multi-tenant: valida que o paciente é do tenant antes de listar.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(q.PacienteId, q.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        var itens = await _repo.ListarDoPaciente(paciente.Id, q.EstabelecimentoId, q.Status);

        if (itens.Count > 0)
        {
            await _audit.RegistrarAsync(
                q.EstabelecimentoId, q.SolicitanteUsuarioId,
                "termo-listou-paciente", "Paciente", paciente.Id,
                metadataJson: $"{{\"qtd\":{itens.Count}}}");
        }

        return itens;
    }
}

public sealed class ObterTermoEmitidoQueryHandlers : IRequestHandler<ObterTermoEmitidoQuery, TermoEmitidoDetalheDto>
{
    private readonly ITermoEmitidoQueryRepository _repo;
    private readonly ITermoAuditLogger _audit;

    public ObterTermoEmitidoQueryHandlers(ITermoEmitidoQueryRepository repo, ITermoAuditLogger audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<TermoEmitidoDetalheDto> Handle(ObterTermoEmitidoQuery q)
    {
        var dto = await _repo.ObterPorIdComSnapshot(q.TermoEmitidoId, q.EstabelecimentoId)
            ?? throw new BusinessException("Termo não encontrado.");

        // Defense-in-depth: dto.PacienteId precisa bater com o que o front afirmou.
        if (q.PacienteId > 0 && dto.PacienteId != q.PacienteId)
            throw new BusinessException("Termo não encontrado.");

        await _audit.RegistrarAsync(
            q.EstabelecimentoId, q.SolicitanteUsuarioId,
            "termo-snapshot-visualizado", "TermoEmitido", dto.Id);

        return dto;
    }
}

public sealed class ObterUrlPdfTermoQueryHandlers : IRequestHandler<ObterUrlPdfTermoQuery, TermoPdfUrlDto>
{
    private readonly ITermoEmitidoRepository _repo;
    private readonly ITermoPdfStorageService _storage;
    private readonly ITermoAuditLogger _audit;
    private const int TtlSegundos = 300;

    public ObterUrlPdfTermoQueryHandlers(
        ITermoEmitidoRepository repo,
        ITermoPdfStorageService storage,
        ITermoAuditLogger audit)
    {
        _repo = repo;
        _storage = storage;
        _audit = audit;
    }

    public async Task<TermoPdfUrlDto> Handle(ObterUrlPdfTermoQuery q)
    {
        var termo = await _repo.ObterPorIdOuNulo(q.TermoEmitidoId, q.EstabelecimentoId)
            ?? throw new BusinessException("Termo não encontrado.");
        if (string.IsNullOrEmpty(termo.PdfUrl))
            throw new BusinessException("Este termo não possui PDF anexado.");

        var url = await _storage.GerarUrlAssinadaLeituraAsync(termo.PdfUrl, TtlSegundos);

        await _audit.RegistrarAsync(
            q.EstabelecimentoId, q.SolicitanteUsuarioId,
            "termo-pdf-baixou", "TermoEmitido", termo.Id);

        return new TermoPdfUrlDto { Url = url, TtlSegundos = TtlSegundos };
    }
}
