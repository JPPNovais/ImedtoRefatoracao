using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Receitas.Commands;

/// <summary>
/// Autosave de rascunho — substitui observações e itens. Não loga audit
/// (rascunho ainda não é Escrita clínica).
/// </summary>
public class AtualizarRascunhoReceitaCommandHandler : ICommandHandler<AtualizarRascunhoReceitaCommand>
{
    private readonly IReceitaRepository _receitaRepo;

    public AtualizarRascunhoReceitaCommandHandler(IReceitaRepository receitaRepo)
    {
        _receitaRepo = receitaRepo;
    }

    public async Task Handle(AtualizarRascunhoReceitaCommand cmd)
    {
        var receita = await _receitaRepo.ObterPorIdOuNulo(cmd.ReceitaId)
            ?? throw new BusinessException("Receita não encontrada.");

        if (receita.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Receita não pertence a este estabelecimento.");
        if (receita.DeletadoEm is not null)
            throw new BusinessException("Receita não encontrada.");
        if (receita.ProfissionalUsuarioId != cmd.SolicitanteUsuarioId)
            throw new BusinessException("Apenas o profissional responsável pode editar este rascunho.");

        var itensRicos = cmd.Itens.Select(ReceitaParsers.ToInput);
        receita.AtualizarRascunho(cmd.Observacoes, itensRicos);

        await _receitaRepo.Salvar(receita);
    }
}
