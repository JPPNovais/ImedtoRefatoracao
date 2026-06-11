namespace Imedto.Backend.Domain.Financeiro;

public interface IConfigComissaoProfissionalRepository
{
    Task<ConfigComissaoProfissional?> ObterOuNulo(long estabelecimentoId, Guid profissionalUsuarioId, TipoComissao tipo);
    Task Salvar(ConfigComissaoProfissional config);
}
