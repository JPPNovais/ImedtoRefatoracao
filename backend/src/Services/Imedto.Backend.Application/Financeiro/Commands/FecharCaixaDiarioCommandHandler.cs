using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Commands;

/// <summary>
/// Fecha o caixa do dia (R9/CA165/CA166).
/// 422 se caixa não existir ou já estiver fechado.
/// </summary>
public class FecharCaixaDiarioCommandHandler : ICommandHandler<FecharCaixaDiarioCommand>
{
    private readonly ICaixaDiarioRepository _repo;

    public FecharCaixaDiarioCommandHandler(ICaixaDiarioRepository repo) => _repo = repo;

    public async Task Handle(FecharCaixaDiarioCommand command)
    {
        var caixa = await _repo.ObterPorData(command.EstabelecimentoId, command.Data)
            ?? throw new BusinessException("Não encontrado.");

        // Fechar chama CaixaDiario.Fechar() que lança 422 se já fechado (CA166).
        caixa.Fechar(command.UsuarioId, command.Observacao);
        await _repo.Salvar(caixa);
    }
}
