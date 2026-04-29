namespace Imedto.Backend.Domain.Catalogo;

public interface IEspecialidadeRepository
{
    Task<Especialidade> ObterPorId(long id);
    Task<Especialidade?> ObterPorIdOuNulo(long id);
    Task Salvar(Especialidade especialidade);
}
