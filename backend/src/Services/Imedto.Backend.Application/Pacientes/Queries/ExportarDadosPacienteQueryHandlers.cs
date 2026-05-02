using Microsoft.Extensions.Logging;
using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Pacientes.Queries;

/// <summary>
/// LGPD Art. 18 — direito à portabilidade. Por ora retorna apenas o paciente em si
/// + metadados de tratamento (cadastro/atualizacao/exclusao/anonimizacao). Nas fases
/// de Prontuário/Agenda/Financeiro esse handler sera ampliado para agregar todos os
/// dados vinculados.
/// </summary>
public class ExportarDadosPacienteQueryHandlers : IRequestHandler<ExportarDadosPacienteQuery, PacienteExportLgpdDto>
{
    private readonly PacienteQueryRepository _queryRepository;
    private readonly ILogger<ExportarDadosPacienteQueryHandlers> _logger;

    public ExportarDadosPacienteQueryHandlers(
        PacienteQueryRepository queryRepository,
        ILogger<ExportarDadosPacienteQueryHandlers> logger)
    {
        _queryRepository = queryRepository;
        _logger = logger;
    }

    public async Task<PacienteExportLgpdDto> Handle(ExportarDadosPacienteQuery query)
    {
        var paciente = await _queryRepository.ObterParaExportLgpd(query.PacienteId, query.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        // LGPD: log estruturado do export — so IDs (sem PII). E indice de auditoria
        // ate o PacienteAcessoLog dedicado ser criado (follow-up).
        _logger.LogInformation(
            "LGPD: export solicitado. Paciente={PacienteId}, Estabelecimento={EstabelecimentoId}",
            query.PacienteId, query.EstabelecimentoId);

        return new PacienteExportLgpdDto
        {
            ExportadoEm = DateTime.UtcNow,
            Paciente = paciente
        };
    }
}
