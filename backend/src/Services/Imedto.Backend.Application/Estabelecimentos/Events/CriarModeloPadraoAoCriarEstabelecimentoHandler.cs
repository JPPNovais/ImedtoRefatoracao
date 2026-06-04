using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Events;

/// <summary>
/// Reage a <see cref="EstabelecimentoCriadoEvent"/> criando as cópias dos modelos de permissão
/// padrão do sistema para o novo estabelecimento.
///
/// Briefing 2026-06-04_001 (CA6): semeia a partir dos registros globais (estabelecimento_id NULL)
/// em vez do hardcode de <c>CriarPadroes()</c>. Quando não há registros globais (ambiente legado
/// sem seed executado), cai de volta para <c>CriarPadroes()</c> como fallback gracioso.
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
        var globais = await _repository.ListarGlobais();

        if (globais.Count > 0)
        {
            // Caminho principal (CA6): semeia cópias a partir dos registros globais.
            foreach (var global in globais)
            {
                var copia = ModeloPermissaoEstabelecimento.CriarCopiaDeGlobal(global, domainEvent.EstabelecimentoId);
                await _repository.Salvar(copia);
            }
        }
        else
        {
            // Fallback para ambientes sem seed executado (retrocompatibilidade).
            foreach (var modelo in ModeloPermissaoEstabelecimento.CriarPadroes(domainEvent.EstabelecimentoId))
                await _repository.Salvar(modelo);
        }
    }
}
