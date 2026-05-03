using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Pacientes.Queries;

/// <summary>
/// LGPD Art. 18 — direito à portabilidade. Por ora retorna apenas o paciente em si
/// + metadados de tratamento (cadastro/atualizacao/exclusao/anonimizacao). Nas fases
/// de Prontuário/Agenda/Financeiro esse handler sera ampliado para agregar todos os
/// dados vinculados.
///
/// Scoped: depende de IPacienteAcessoLogService (audit LGPD).
/// </summary>
public class ExportarDadosPacienteQueryHandlers : IRequestHandler<ExportarDadosPacienteQuery, PacienteExportLgpdDto>
{
    private readonly PacienteQueryRepository _queryRepository;
    private readonly IPacienteAcessoLogService _acessoLog;

    public ExportarDadosPacienteQueryHandlers(
        PacienteQueryRepository queryRepository,
        IPacienteAcessoLogService acessoLog)
    {
        _queryRepository = queryRepository;
        _acessoLog = acessoLog;
    }

    public async Task<PacienteExportLgpdDto> Handle(ExportarDadosPacienteQuery query)
    {
        var paciente = await _queryRepository.ObterParaExportLgpd(query.PacienteId, query.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        // Audit LGPD obrigatorio (Art. 37): export de dados pessoais eh evento sensivel.
        await _acessoLog.RegistrarAsync(
            query.PacienteId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoPaciente.Export);

        return new PacienteExportLgpdDto
        {
            ExportadoEm = DateTime.UtcNow,
            Paciente = paciente
        };
    }
}
