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

        var modelo = ModeloPermissaoEstabelecimento.Criar(
            cmd.EstabelecimentoId,
            cmd.Nome,
            tipoAcesso,
            cmd.Permissoes);

        await _repo.Salvar(modelo);
        cmd.ModeloIdCriado = modelo.Id;
    }
}
