using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

/// <summary>
/// Reabre o caixa fechado — apenas para o Dono (R10/CA167).
/// RBAC: controller verifica financeiro.fechar; o handler verifica EhDono adicionalmente.
/// </summary>
public class ReabrirCaixaDiarioCommandHandler : ICommandHandler<ReabrirCaixaDiarioCommand>
{
    private readonly ICaixaDiarioRepository _repo;

    public ReabrirCaixaDiarioCommandHandler(ICaixaDiarioRepository repo) => _repo = repo;

    public async Task Handle(ReabrirCaixaDiarioCommand command)
    {
        // Verificação de papel: apenas o Dono pode reabrir (R10/CA167).
        if (!command.EhDono)
            throw new BusinessException("Apenas o Dono pode reabrir o caixa.");

        var caixa = await _repo.ObterPorData(command.EstabelecimentoId, command.Data)
            ?? throw new BusinessException("Não encontrado.");

        // Reabrir chama CaixaDiario.Reabrir() que lança se já Aberto.
        caixa.Reabrir(command.UsuarioId);
        await _repo.Salvar(caixa);
    }
}
