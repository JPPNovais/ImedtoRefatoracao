namespace Imedto.Backend.Domain.Prontuarios;

/// <summary>
/// Severidade de um achado em uma região do exame físico.
/// Persistido como string no banco (varchar(20)) para preservar legibilidade
/// e tolerar futuras adições sem migration de dados.
/// </summary>
public enum SeveridadeExame
{
    Normal,
    LeveAlteracao,
    Alterado,
    Critico
}

/// <summary>
/// Lateralidade de uma região anatômica. Reflete a flag <c>lateralidade</c> do legado
/// — algumas regiões (olho, mama, pulmão, membro) admitem D/E/Bilateral; outras
/// (epigástrio, mesogástrio, etc.) são <see cref="NaoAplicavel"/>.
/// </summary>
public enum Lateralidade
{
    NaoAplicavel,
    Esquerda,
    Direita,
    Bilateral
}
