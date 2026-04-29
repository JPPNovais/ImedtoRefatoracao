using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cirurgias.Commands;

public class CancelarProcedimentoCommandHandler : ICommandHandler<CancelarProcedimentoCommand>
{
    private readonly IProcedimentoCirurgicoRepository _repo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public CancelarProcedimentoCommandHandler(
        IProcedimentoCirurgicoRepository repo,
        IProntuarioAcessoLogService acessoLog)
    {
        _repo = repo;
        _acessoLog = acessoLog;
    }

    public async Task Handle(CancelarProcedimentoCommand cmd)
    {
        var procedimento = await _repo.ObterPorId(cmd.ProcedimentoId);
        if (procedimento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Procedimento não pertence a este estabelecimento.");

        procedimento.Cancelar(cmd.Motivo);
        await _repo.Salvar(procedimento);

        if (cmd.SolicitanteUsuarioId != Guid.Empty)
        {
            await _acessoLog.RegistrarAsync(
                procedimento.ProntuarioId, cmd.SolicitanteUsuarioId, cmd.EstabelecimentoId,
                TipoAcessoProntuario.Escrita);
        }
    }
}
