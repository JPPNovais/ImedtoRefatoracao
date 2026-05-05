namespace Imedto.Backend.Domain.Usuarios;

public interface IUsuarioRepository
{
    Task<Usuario> ObterPorId(Guid id);
    Task<Usuario> ObterPorIdOuNulo(Guid id);
    /// <summary>
    /// Variante somente-leitura (AsNoTracking). Use em endpoints/filtros que apenas leem
    /// o aggregate — evita a alocação do change tracker do EF. NUNCA use em command
    /// handlers que modificam o usuário: alterações nessa instância não serão persistidas.
    /// </summary>
    Task<Usuario> ObterPorIdParaLeitura(Guid id);
    Task<bool> ExisteCpf(string cpf, Guid ignorarUsuarioId);
    Task Salvar(Usuario usuario);
}
