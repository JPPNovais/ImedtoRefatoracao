namespace Imedto.Backend.Domain.Agendamentos;

public interface IAgendamentoRepository
{
    Task<Agendamento> ObterPorId(long id);
    Task<Agendamento?> ObterPorIdOuNulo(long id);
    Task Salvar(Agendamento agendamento);

    /// <summary>
    /// Verifica se o profissional já tem um agendamento ativo que se sobreponha ao intervalo indicado.
    /// </summary>
    Task<bool> ExisteConflito(
        Guid profissionalUsuarioId,
        DateTime inicioPrevisto,
        DateTime fimPrevisto,
        long? excluirAgendamentoId = null);
}
