namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Repositório de escrita do exame físico (EF Core). Carrega o aggregate
/// completo (com a coleção de regiões) — necessário para validar invariantes
/// no <see cref="ExameFisico.AtualizarRegiao"/>/<see cref="ExameFisico.RemoverRegiao"/>.
/// </summary>
public interface IExameFisicoRepository
{
    /// <summary>Carrega o aggregate completo (com regiões). Lança se não encontrado.</summary>
    Task<ExameFisico> ObterPorId(long id);

    /// <summary>Carrega o aggregate completo (com regiões) ou retorna null.</summary>
    Task<ExameFisico?> ObterPorIdOuNulo(long id);

    Task Salvar(ExameFisico exame);
}
