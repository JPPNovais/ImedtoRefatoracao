using Imedto.Backend.Contracts.PedidosExame.Queries;
using Imedto.Backend.Contracts.PedidosExame.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.PedidosExame.Queries;

public class ListarPedidosExameDoPacienteQueryHandlers
    : IRequestHandler<ListarPedidosExameDoPacienteQuery, IReadOnlyList<PedidoExameDto>>
{
    private readonly IPedidoExameQueryRepository _queryRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ListarPedidosExameDoPacienteQueryHandlers(
        IPedidoExameQueryRepository queryRepo,
        IPacienteRepository pacienteRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepo = queryRepo;
        _pacienteRepo = pacienteRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
    }

    public async Task<IReadOnlyList<PedidoExameDto>> Handle(ListarPedidosExameDoPacienteQuery query)
    {
        if (query.PacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");

        var paciente = await _pacienteRepo.ObterPorIdOuNulo(query.PacienteId, query.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        var pedidos = await _queryRepo.ListarDoPaciente(query.PacienteId, query.EstabelecimentoId);

        var prontuario = await _prontuarioRepo.ObterPorPaciente(paciente.Id, query.EstabelecimentoId);
        if (prontuario is not null && pedidos.Count > 0)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);
        }

        return pedidos;
    }
}

public class ObterPedidoExameQueryHandlers : IRequestHandler<ObterPedidoExameQuery, PedidoExameDto>
{
    private readonly IPedidoExameQueryRepository _queryRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ObterPedidoExameQueryHandlers(
        IPedidoExameQueryRepository queryRepo,
        IProntuarioRepository prontuarioRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepo = queryRepo;
        _prontuarioRepo = prontuarioRepo;
        _acessoLog = acessoLog;
    }

    public async Task<PedidoExameDto> Handle(ObterPedidoExameQuery query)
    {
        var pedido = await _queryRepo.ObterPorId(query.PedidoExameId, query.EstabelecimentoId)
            ?? throw new BusinessException("Pedido de exame não encontrado.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(pedido.PacienteId, query.EstabelecimentoId);
        if (prontuario is not null)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);
        }

        return pedido;
    }
}
