using Imedto.Backend.Contracts.ModelosPermissao.Commands;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.ModelosPermissao.Commands;

public class CriarModeloPermissaoCommandHandler : ICommandHandler<CriarModeloPermissaoCommand>
{
    private readonly IModeloPermissaoRepository _repo;

    public CriarModeloPermissaoCommandHandler(IModeloPermissaoRepository repo)
        => _repo = repo;

    public async Task Handle(CriarModeloPermissaoCommand cmd)
    {
        if (!Enum.TryParse<TipoAcessoModelo>(cmd.TipoAcesso, out var tipoAcesso))
            throw new BusinessException($"TipoAcesso inválido: '{cmd.TipoAcesso}'. Use 'Profissional' ou 'Recepcionista'.");

        // Pré-valida unicidade pra retornar 422 limpo — sem isso, a unique constraint
        // do DB lança DbUpdateException que cai no handler global como 500 ErroInterno.
        if (await _repo.ExisteComNomeNoEstabelecimento(cmd.Nome, cmd.EstabelecimentoId))
            throw new BusinessException("Já existe um modelo de permissão com este nome.");

        var modelo = ModeloPermissaoEstabelecimento.Criar(
            cmd.EstabelecimentoId,
            cmd.Nome,
            tipoAcesso,
            cmd.Permissoes,
            permissoesExtras: null,
            icone: cmd.Icone,
            cor: cmd.Cor,
            descricao: cmd.Descricao);

        await _repo.Salvar(modelo);
        cmd.ModeloIdCriado = modelo.Id;
    }
}
