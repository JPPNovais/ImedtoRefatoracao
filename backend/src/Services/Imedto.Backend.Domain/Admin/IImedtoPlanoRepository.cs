namespace Imedto.Backend.Domain.Admin;

public interface IImedtoPlanoRepository
{
    Task<ImedtoPlano?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExisteNomeAsync(string nome, Guid? excluindoId = null, CancellationToken ct = default);
    void Adicionar(ImedtoPlano plano);
    void Atualizar(ImedtoPlano plano);
}
