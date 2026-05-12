using Imedto.Backend.Contracts.ModelosPermissao.Commands;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.ModelosPermissao.Commands;

public class AtualizarModeloPermissaoCommandHandler : ICommandHandler<AtualizarModeloPermissaoCommand>
{
    private readonly IModeloPermissaoRepository _repo;

    public AtualizarModeloPermissaoCommandHandler(IModeloPermissaoRepository repo)
        => _repo = repo;

    public async Task Handle(AtualizarModeloPermissaoCommand cmd)
    {
        if (!Enum.TryParse<TipoAcessoModelo>(cmd.TipoAcesso, out var tipoAcesso))
            throw new BusinessException($"TipoAcesso inválido: '{cmd.TipoAcesso}'.");

        var modelo = await _repo.ObterPorIdOuNulo(cmd.ModeloId)
            ?? throw new BusinessException("Modelo não encontrado.");

        // Defense-in-depth multi-tenant: mensagem padronizada (nao vaza existencia).
        if (modelo.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Modelo não encontrado.");

        // Pré-valida unicidade (excluindo o próprio id) pra retornar 422 limpo.
        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.Nome, cmd.EstabelecimentoId, excetoId: modelo.Id))
            throw new BusinessException("Já existe um modelo de permissão com este nome.");

        // O domínio já valida que modelos padrão não podem ser editados.
        modelo.Atualizar(
            cmd.Nome,
            tipoAcesso,
            cmd.Permissoes,
            permissoesExtras: null,
            icone: cmd.Icone,
            cor: cmd.Cor,
            descricao: cmd.Descricao);
        await _repo.Salvar(modelo);
    }
}
