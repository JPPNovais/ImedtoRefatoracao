namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Categorias pré-cadastradas que o prontuário pode referenciar em seus campos de lista.
/// Tipos válidos: Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar, Expectativa.
/// Droga e AtividadeFisica foram removidos (sem campo no prontuário — briefing 2026-06-05_001).
/// </summary>
public enum TipoVariavelPool
{
    Alergia,
    Medicamento,
    Doenca,
    Cirurgia,
    RelacaoFamiliar,
    Expectativa
}
