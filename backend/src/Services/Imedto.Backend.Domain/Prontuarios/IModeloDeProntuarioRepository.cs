namespace Imedto.Backend.Domain.Prontuarios;

public interface IModeloDeProntuarioRepository
{
    /// <summary>
    /// Carrega um modelo VISÍVEL ao estabelecimento — padrão do sistema OU pertencente ao tenant.
    /// Defense-in-depth IDOR: bloqueia acesso a modelos privados de outros tenants.
    /// Caller que pretende editar/excluir deve checar <c>EhPadraoSistema</c> + igualdade de tenant.
    /// </summary>
    Task<ModeloDeProntuario?> ObterVisivelOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Retorna o primeiro modelo ativo visível ao estabelecimento (próprio ou padrão-sistema),
    /// usado como fallback quando o caller não especifica um modelo (ex.: app mobile).
    /// Prioriza modelo do próprio estabelecimento; fallback para padrão-sistema.
    /// Retorna null quando não há nenhum modelo disponível.
    /// </summary>
    Task<ModeloDeProntuario?> ObterPrimeiroVisivelOuNulo(long estabelecimentoId);

    Task Salvar(ModeloDeProntuario modelo);
    Task Excluir(ModeloDeProntuario modelo);
}
