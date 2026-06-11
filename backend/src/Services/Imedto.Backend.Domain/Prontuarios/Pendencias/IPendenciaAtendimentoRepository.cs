namespace Imedto.Backend.Domain.Prontuarios.Pendencias;

public interface IPendenciaAtendimentoRepository
{
    /// <summary>
    /// Salva a pendência (INSERT). A UNIQUE (evolucao_id, acao) no banco trava duplicatas;
    /// o chamador deve verificar existência antes ou tratar a exceção de constraint.
    /// </summary>
    Task Salvar(PendenciaAtendimento pendencia);

    /// <summary>
    /// Retorna a pendência mais recente com status=Pendente para o paciente+tenant+ação.
    /// Usado pelos event handlers de conclusão automática (R7-R11).
    /// Retorna null se não houver pendência aberta (handler faz no-op — R12/CA65).
    /// Falha-fechada: sem tenant claim → não chama este método (repo não valida claim).
    /// </summary>
    Task<PendenciaAtendimento?> ObterAbertaMaisRecentePorAcao(
        long estabelecimentoId,
        long pacienteId,
        AcaoPendencia acao);

    /// <summary>
    /// Retorna uma pendência por id com filtro de tenant (R5).
    /// Retorna null se não encontrada ou tenant diferente.
    /// </summary>
    Task<PendenciaAtendimento?> ObterPorId(long id, long estabelecimentoId);

    /// <summary>
    /// Verifica se já existe uma pendência para o par (evolucao_id, acao).
    /// Usado pelo PendenciaExtratorEvolucao para idempotência antes de tentar inserir (CA62).
    /// </summary>
    Task<bool> ExistePorEvolucaoEAcao(long evolucaoId, AcaoPendencia acao);
}
