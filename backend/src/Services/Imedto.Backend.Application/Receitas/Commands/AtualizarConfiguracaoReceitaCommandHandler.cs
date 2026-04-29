using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Receitas.Commands;

/// <summary>
/// Cria ou atualiza a configuração de receita do estabelecimento. A trava de "só dono"
/// é feita no controller via <c>[RequiresPapel(TenantPapel.Dono)]</c>; aqui o handler
/// é idempotente — se não existir registro, cria; se existir, atualiza.
/// </summary>
public class AtualizarConfiguracaoReceitaCommandHandler : ICommandHandler<AtualizarConfiguracaoReceitaCommand>
{
    private readonly IConfiguracaoReceitaRepository _repo;

    public AtualizarConfiguracaoReceitaCommandHandler(IConfiguracaoReceitaRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(AtualizarConfiguracaoReceitaCommand cmd)
    {
        var config = await _repo.ObterPorEstabelecimentoOuNulo(cmd.EstabelecimentoId)
                     ?? ConfiguracaoReceitaEstabelecimento.CriarPadrao(cmd.EstabelecimentoId);

        config.Atualizar(
            cmd.CabecalhoHtml,
            cmd.RodapeHtml,
            cmd.ModeloPadraoId,
            cmd.EmissorPadrao);

        await _repo.Salvar(config);
    }
}
