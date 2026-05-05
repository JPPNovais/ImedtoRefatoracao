namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Repositório de escrita do exame físico (EF Core). Carrega o aggregate
/// completo (com a coleção de regiões) — necessário para validar invariantes
/// no <see cref="ExameFisico.AtualizarRegiao"/>/<see cref="ExameFisico.RemoverRegiao"/>.
/// </summary>
public interface IExameFisicoRepository
{
    /// <summary>
    /// Carrega o aggregate completo (com regiões) filtrando por
    /// <paramref name="estabelecimentoId"/> (defense-in-depth IDOR/LGPD).
    /// Retorna null se inexistente ou de outro tenant.
    /// </summary>
    Task<ExameFisico?> ObterPorIdOuNulo(long id, long estabelecimentoId);

    Task Salvar(ExameFisico exame);
}
