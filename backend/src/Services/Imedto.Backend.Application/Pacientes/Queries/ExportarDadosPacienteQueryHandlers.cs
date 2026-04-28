using Microsoft.Extensions.Logging;
using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Pacientes.Queries;

/// <summary>
/// LGPD Art. 18 — direito à portabilidade. Por ora retorna apenas o paciente em si;
/// nas fases de Prontuário / Agenda / Financeiro esse handler será ampliado para agregar
/// todos os dados vinculados (prontuários, consultas, anexos, etc.).
/// </summary>
public class ExportarDadosPacienteQueryHandlers : IRequestHandler<ExportarDadosPacienteQuery, object>
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

    public async Task<object> Handle(ExportarDadosPacienteQuery query)
    {
        var paciente = await _queryRepository.ObterPorId(query.PacienteId, query.EstabelecimentoId);
        if (paciente is null)
            throw new BusinessException("Paciente não encontrado neste estabelecimento.");

        _logger.LogInformation(
            "LGPD: export solicitado. Paciente={PacienteId}, Estabelecimento={EstabelecimentoId}",
            query.PacienteId, query.EstabelecimentoId);

        return new
        {
            exportadoEm = DateTime.UtcNow,
            paciente,
            // prontuarios = ...   // Fase 3
            // agenda      = ...   // Fase 4
            // financeiro  = ...   // Fase 8
        };
    }
}
