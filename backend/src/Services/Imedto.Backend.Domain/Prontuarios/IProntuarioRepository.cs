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
}
