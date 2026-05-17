namespace Imedto.Backend.Domain.Prontuarios;

public interface IProntuarioRepository
{
    /// <summary>
    /// Carrega o prontuário filtrando por <paramref name="estabelecimentoId"/>
    /// (defense-in-depth IDOR/LGPD). Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<Prontuario?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    Task<Prontuario> ObterPorPaciente(long pacienteId, long estabelecimentoId);
    Task Salvar(Prontuario prontuario);
}

public interface IProntuarioEvolucaoRepository
{
    Task Salvar(ProntuarioEvolucao evolucao);

    /// <summary>
    /// Carrega a evolução validando que ela pertence ao prontuário informado
    /// (defense-in-depth IDOR/LGPD). Retorna null se não existir ou se o
    /// vínculo prontuário↔evolução não bater — o handler converte em mensagem
    /// genérica "Evolução não encontrada." para não revelar existência cross-tenant.
    /// </summary>
    Task<ProntuarioEvolucao?> ObterDoProntuarioOuNulo(long evolucaoId, long prontuarioId);
}
