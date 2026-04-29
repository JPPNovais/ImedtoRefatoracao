namespace Imedto.Backend.Domain.Assinaturas;

public interface IAssinaturaRepository
{
    Task<Assinatura?> ObterPorEstabelecimentoOuNulo(long estabelecimentoId);

    Task Salvar(Assinatura assinatura);

    /// <summary>
    /// Lista assinaturas em <see cref="StatusAssinatura.Trial"/> cuja
    /// <c>ExpiraEm &lt;= ate</c>. Usado pelo job de expiração de trials.
    /// </summary>
    Task<List<Assinatura>> ListarTrialsExpirando(DateTime ate);
}
