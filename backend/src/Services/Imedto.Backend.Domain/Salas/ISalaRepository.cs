namespace Imedto.Backend.Domain.Salas;

public interface ISalaRepository
{
    Task<Sala> ObterPorId(long id);
    Task<Sala?> ObterPorIdOuNulo(long id);
    Task<bool> ExisteOutraComMesmoNome(long estabelecimentoId, string nome, long ignorarId);
    Task Salvar(Sala sala);
    Task Excluir(Sala sala);
}
