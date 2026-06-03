namespace Imedto.Backend.Domain.Agendamentos;

public interface IAgendamentoRepository
{
    /// <summary>
    /// Carrega o agendamento filtrando por <paramref name="estabelecimentoId"/> (defense-in-depth
    /// LGPD/IDOR: nao confia que o handler vai checar depois). Retorna null se inexistente
    /// ou de outro tenant — em ambos os casos o handler deve responder "não encontrado"
    /// para nao vazar existencia cross-tenant.
    /// </summary>
    Task<Agendamento?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    /// <summary>
    /// Carrega o agendamento pelo token de confirmação pública (Fase 2).
    /// NÃO filtra por tenant — o token (256 bits) é o segredo único.
    /// Retorna null quando o token não existe (sem PII nos erros — controller devolve 410 genérico).
    /// </summary>
    Task<Agendamento?> ObterPorTokenOuNulo(string token);

    Task Salvar(Agendamento agendamento);

    /// <summary>
    /// Persiste log de acesso ao link público (R16).
    /// Append-only — sem PII do paciente (IP/UA/acao/AgendamentoId/EstabelecimentoId).
    /// </summary>
    Task SalvarAcessoLog(AgendamentoConfirmacaoAcessoLog log);

    /// <summary>
    /// Verifica se o profissional já tem um agendamento ativo no estabelecimento informado
    /// que se sobreponha ao intervalo indicado. Filtra por estabelecimento porque um
    /// profissional que atua em mais de um tenant tem agendas independentes.
    /// </summary>
    Task<bool> ExisteConflito(
        long estabelecimentoId,
        Guid profissionalUsuarioId,
        DateTime inicioPrevisto,
        DateTime fimPrevisto,
        long? excluirAgendamentoId = null);
}
