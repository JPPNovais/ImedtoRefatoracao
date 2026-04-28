using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Domain.Unidades;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Events;

/// <summary>
/// Reage a <see cref="EstabelecimentoCriadoEvent"/> criando a unidade principal "Sede",
/// para que o estabelecimento nunca fique sem unidade logo após o cadastro.
/// </summary>
public class CriarUnidadePadraoAoCriarEstabelecimentoHandler : IEventHandler<EstabelecimentoCriadoEvent>
{
    private readonly IUnidadeRepository _repository;

    public CriarUnidadePadraoAoCriarEstabelecimentoHandler(IUnidadeRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(EstabelecimentoCriadoEvent @event)
    {
        var unidade = UnidadeEstabelecimento.Criar(
            @event.EstabelecimentoId,
            "Sede",
            isPrincipal: true,
            new EnderecoUnidadeInput(null, null, null, null, null, null, null),
            telefone: null);

        await _repository.Salvar(unidade);
    }
}
