using Imedto.Backend.Contracts.PedidosExame.Queries;
using Imedto.Backend.Contracts.PedidosExame.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.PedidosExame.Queries;

public class ListarPedidosExameDoPacienteQueryHandlers
    : IRequestHandler<ListarPedidosExameDoPacienteQuery, PaginaPedidosExameDto>
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

    public async Task<PaginaPedidosExameDto> Handle(ListarPedidosExameDoPacienteQuery query)
    {
        if (query.PacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");

        var pagina = query.Pagina < 1 ? 1 : query.Pagina;
        var tamanho = query.TamanhoPagina is < 1 or > 100 ? 20 : query.TamanhoPagina;

        var paciente = await _pacienteRepo.ObterPorIdOuNulo(query.PacienteId, query.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        var resultado = await _queryRepo.ListarDoPaciente(
            query.PacienteId, query.EstabelecimentoId, pagina, tamanho,
            query.SolicitanteUsuarioId, query.SolicitantePapel);

        var prontuario = await _prontuarioRepo.ObterPorPaciente(paciente.Id, query.EstabelecimentoId);
        if (prontuario is not null && resultado.Total > 0)
        {
            await _acessoLog.RegistrarAsync(
                prontuario.Id, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoProntuario.Leitura);
        }

        return resultado;
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
        var pedido = await _queryRepo.ObterPorId(
            query.PedidoExameId, query.EstabelecimentoId,
            query.SolicitanteUsuarioId, query.SolicitantePapel)
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
