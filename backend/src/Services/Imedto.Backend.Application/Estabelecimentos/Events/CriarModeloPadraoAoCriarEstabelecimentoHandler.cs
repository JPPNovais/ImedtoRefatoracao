using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Events;

/// <summary>
/// Reage a <see cref="EstabelecimentoCriadoEvent"/> criando os 3 modelos de permissão
/// padrão (Admin / Médico / Recepção), replicando o comportamento do legado.
/// Cada um deles é marcado como <c>EhPadrao = true</c> para impedir edição/exclusão.
/// </summary>
public class CriarModeloPadraoAoCriarEstabelecimentoHandler : IEventHandler<EstabelecimentoCriadoEvent>
{
    private readonly IModeloPermissaoRepository _repository;

    public CriarModeloPadraoAoCriarEstabelecimentoHandler(IModeloPermissaoRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(EstabelecimentoCriadoEvent domainEvent)
    {
        foreach (var modelo in ModeloPermissaoEstabelecimento.CriarPadroes(domainEvent.EstabelecimentoId))
            await _repository.Salvar(modelo);
    }
}
