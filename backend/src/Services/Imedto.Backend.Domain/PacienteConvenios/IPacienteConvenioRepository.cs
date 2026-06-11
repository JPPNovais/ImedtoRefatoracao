namespace Imedto.Backend.Domain.PacienteConvenios;

public interface IPacienteConvenioRepository
{
    Task<PacienteConvenio?> ObterPorIdOuNulo(long id, long estabelecimentoId);
    Task Salvar(PacienteConvenio pacienteConvenio);
    Task Excluir(PacienteConvenio pacienteConvenio);
}
