using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Pacientes.Queries;

/// <summary>
/// Retorna CPF e telefone completos de um paciente com trilha de auditoria LGPD.
///
/// Scoped — depende de <see cref="IPacienteAcessoLogService"/> (scoped).
/// Multi-tenant: filtro por estabelecimento_id delegado ao repositório (falha-fechada).
/// Mensagem de erro genérica: 404 sem revelar se o paciente existe em outro tenant.
/// </summary>
public class ObterDadosSensiveisPacienteQueryHandler
    : IRequestHandler<ObterDadosSensiveisPacienteQuery, DadosSensiveisPacienteDto>
{
    private readonly PacienteQueryRepository _queryRepository;
    private readonly IPacienteAcessoLogService _acessoLog;

    public ObterDadosSensiveisPacienteQueryHandler(
        PacienteQueryRepository queryRepository,
        IPacienteAcessoLogService acessoLog)
    {
        _queryRepository = queryRepository;
        _acessoLog = acessoLog;
    }

    public async Task<DadosSensiveisPacienteDto> Handle(ObterDadosSensiveisPacienteQuery query)
    {
        var dados = await _queryRepository.ObterDadosSensiveis(
            query.PacienteId, query.EstabelecimentoId);

        if (dados is null) return null;

        // Audit LGPD: revelação de PII sensível — sempre registrar ANTES de retornar.
        await _acessoLog.RegistrarAsync(
            query.PacienteId,
            query.SolicitanteUsuarioId,
            query.EstabelecimentoId,
            TipoAcessoPaciente.RevelacaoDadosSensiveis);

        return new DadosSensiveisPacienteDto
        {
            Cpf = dados.Value.Cpf,
            Telefone = dados.Value.Telefone
        };
    }
}
