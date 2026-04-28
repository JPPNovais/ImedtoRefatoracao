namespace Imedto.Backend.Domain.Estabelecimentos;

public interface IEstabelecimentoRepository
{
    Task<Estabelecimento> ObterPorId(long id);
    Task<Estabelecimento> ObterPorIdOuNulo(long id);
    Task<bool> ExisteCnpj(string cnpj, long ignorarEstabelecimentoId);
    Task<bool> UsuarioJaEhDono(Guid usuarioId);
    Task Salvar(Estabelecimento estabelecimento);
}
