namespace Imedto.Backend.Domain.Profissionais;

public interface IProfissionalRepository
{
    Task<Profissional> ObterPorId(Guid usuarioId);
    Task<Profissional> ObterPorIdOuNulo(Guid usuarioId);
    Task<bool> ExisteConselhoRegistro(string conselho, string uf, string numeroRegistro, Guid ignorarUsuarioId);
    Task Salvar(Profissional profissional);
}
