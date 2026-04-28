namespace Imedto.Backend.Domain.Usuarios;

public interface IUsuarioRepository
{
    Task<Usuario> ObterPorId(Guid id);
    Task<Usuario> ObterPorIdOuNulo(Guid id);
    Task<bool> ExisteCpf(string cpf, Guid ignorarUsuarioId);
    Task Salvar(Usuario usuario);
}
