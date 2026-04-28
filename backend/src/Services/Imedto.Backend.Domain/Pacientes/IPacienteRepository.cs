namespace Imedto.Backend.Domain.Pacientes;

public interface IPacienteRepository
{
    Task<Paciente> ObterPorId(long id);
    Task<Paciente> ObterPorIdOuNulo(long id);
    Task<bool> ExisteCpfNoEstabelecimento(string cpf, long estabelecimentoId, long ignorarPacienteId);
    Task Salvar(Paciente paciente);
}
