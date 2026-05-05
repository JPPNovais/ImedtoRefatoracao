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
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var procedimento = await _repo.ObterPorIdOuNulo(cmd.ProcedimentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Procedimento não encontrado.");

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
