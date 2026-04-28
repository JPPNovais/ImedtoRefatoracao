namespace Imedto.Backend.Domain.ModelosPermissao;

public interface IModeloPermissaoRepository
{
    Task<ModeloPermissaoEstabelecimento> ObterPorId(long id);
    Task<ModeloPermissaoEstabelecimento> ObterPadraoDoEstabelecimento(long estabelecimentoId);
    Task<bool> PertenceAoEstabelecimento(long modeloId, long estabelecimentoId);
    Task<bool> EstaEmUsoPorVinculoAtivo(long modeloId);
    Task Salvar(ModeloPermissaoEstabelecimento modelo);
    Task Excluir(ModeloPermissaoEstabelecimento modelo);
}
