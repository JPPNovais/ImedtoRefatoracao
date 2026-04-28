namespace Imedto.Backend.Domain.Prontuarios;

public interface IModeloDeProntuarioRepository
{
    Task<ModeloDeProntuario> ObterPorId(long id);
    Task<ModeloDeProntuario> ObterPorIdOuNulo(long id);
    Task Salvar(ModeloDeProntuario modelo);
    Task Excluir(ModeloDeProntuario modelo);
}
