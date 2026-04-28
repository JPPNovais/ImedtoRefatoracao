namespace Imedto.Backend.Domain.Prontuarios;

public interface IProntuarioRepository
{
    Task<Prontuario> ObterPorId(long id);
    Task<Prontuario> ObterPorPaciente(long pacienteId, long estabelecimentoId);
    Task Salvar(Prontuario prontuario);
}

public interface IProntuarioEvolucaoRepository
{
    Task Salvar(ProntuarioEvolucao evolucao);
}
