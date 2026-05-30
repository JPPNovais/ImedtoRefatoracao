namespace Imedto.Backend.Domain.Admin;

public interface IImedtoAssinaturaRepository
{
    Task<ImedtoAssinatura?> ObterVigenteDoEstabelecimentoAsync(long estabelecimentoId, CancellationToken ct = default);
    Task<ImedtoAssinatura?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    void Adicionar(ImedtoAssinatura assinatura);
    void Atualizar(ImedtoAssinatura assinatura);
}
