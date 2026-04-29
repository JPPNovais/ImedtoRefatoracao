namespace Imedto.Backend.Domain.Cirurgias;

/// <summary>
/// Papel operacional do membro da equipe na cirurgia. Diferente de
/// <c>OrcamentoEquipe</c> (que carrega comissão/valor) — aqui é puramente operacional.
/// Validação: ao realizar a cirurgia, é obrigatório existir ao menos um <see cref="Cirurgiao"/>.
/// </summary>
public enum PapelCirurgia
{
    Cirurgiao,
    Auxiliar,
    Anestesista,
    Instrumentador,
    Circulante
}
