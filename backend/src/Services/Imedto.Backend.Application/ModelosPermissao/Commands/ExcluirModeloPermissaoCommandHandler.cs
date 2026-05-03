using Imedto.Backend.Contracts.ModelosPermissao.Commands;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.ModelosPermissao.Commands;

public class ExcluirModeloPermissaoCommandHandler : ICommandHandler<ExcluirModeloPermissaoCommand>
{
    private readonly IModeloPermissaoRepository _repo;

    public ExcluirModeloPermissaoCommandHandler(IModeloPermissaoRepository repo)
        => _repo = repo;

    public async Task Handle(ExcluirModeloPermissaoCommand cmd)
    {
        var modelo = await _repo.ObterPorIdOuNulo(cmd.ModeloId)
            ?? throw new BusinessException("Modelo não encontrado.");

        // Defense-in-depth multi-tenant: mensagem padronizada.
        if (modelo.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Modelo não encontrado.");

        modelo.GarantirPodeExcluir();

        if (await _repo.EstaEmUsoPorVinculoAtivo(cmd.ModeloId))
            throw new BusinessException("Não é possível excluir: há profissionais vinculados a este modelo.");

        await _repo.Excluir(modelo);
    }
}
