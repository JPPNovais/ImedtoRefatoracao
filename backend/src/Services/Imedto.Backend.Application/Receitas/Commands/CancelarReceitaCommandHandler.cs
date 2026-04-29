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
        var receita = await _receitaRepo.ObterPorIdOuNulo(cmd.ReceitaId)
            ?? throw new BusinessException("Receita não encontrada.");

        // Defense-in-depth multi-tenant: o RequiresEstabelecimento já valida o header,
        // mas a receita carrega um EstabelecimentoId próprio — checar é barato.
        if (receita.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Receita não pertence a este estabelecimento.");

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
