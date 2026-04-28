namespace Imedto.Backend.Domain.Automacoes;

public interface IConfiguracaoAutomacaoRepository
{
    Task<ConfiguracaoAutomacao?> ObterPorEstabelecimento(long estabelecimentoId);
    Task Salvar(ConfiguracaoAutomacao config);
}
