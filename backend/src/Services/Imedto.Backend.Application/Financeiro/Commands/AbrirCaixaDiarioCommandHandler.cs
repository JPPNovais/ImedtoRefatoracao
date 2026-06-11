using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

/// <summary>
/// Abre o caixa do dia (R7/CA162).
/// Idempotente: se o caixa do dia já existe e está Aberto, retorna sem erro.
/// Duplicar Abrir num dia já com caixa Aberto → 422 "Caixa do dia já está aberto".
/// </summary>
public class AbrirCaixaDiarioCommandHandler : ICommandHandler<AbrirCaixaDiarioCommand>
{
    private readonly ICaixaDiarioRepository _repo;

    public AbrirCaixaDiarioCommandHandler(ICaixaDiarioRepository repo) => _repo = repo;

    public async Task Handle(AbrirCaixaDiarioCommand command)
    {
        var existente = await _repo.ObterPorData(command.EstabelecimentoId, command.Data);

        if (existente is not null)
        {
            if (existente.Status == StatusCaixaDiario.Aberto)
                throw new BusinessException("Caixa do dia já está aberto.");
            // Se existente e Fechado → também não abre (deve usar Reabrir).
            throw new BusinessException("Caixa do dia já existe. Use 'Reabrir' para reabrir um caixa fechado.");
        }

        var caixa = CaixaDiario.Abrir(command.EstabelecimentoId, command.Data, command.UsuarioId);
        await _repo.Salvar(caixa);
    }
}
