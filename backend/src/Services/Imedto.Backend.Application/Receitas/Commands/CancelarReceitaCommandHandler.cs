using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Receitas.Commands;

public class CancelarReceitaCommandHandler : ICommandHandler<CancelarReceitaCommand>
{
    private readonly IReceitaRepository _receitaRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public CancelarReceitaCommandHandler(
        IReceitaRepository receitaRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _receitaRepo = receitaRepo;
        _acessoLog = acessoLog;
    }

    public async Task Handle(CancelarReceitaCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var receita = await _receitaRepo.ObterPorIdOuNulo(cmd.ReceitaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Receita não encontrada.");

        if (receita.DeletadoEm is not null)
            throw new BusinessException("Receita não encontrada.");

        receita.Cancelar(cmd.Motivo);
        await _receitaRepo.Salvar(receita);

        await _acessoLog.RegistrarAsync(
            receita.ProntuarioId,
            cmd.SolicitanteUsuarioId,
            cmd.EstabelecimentoId,
            TipoAcessoProntuario.Escrita);
    }
}
