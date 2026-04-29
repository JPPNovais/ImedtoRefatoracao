namespace Imedto.Backend.Domain.Jobs;

/// <summary>
/// Estados de um <see cref="JobAgendado"/> no ciclo de execução do scheduler.
/// Persistido como string no Postgres para legibilidade direta na tabela.
/// </summary>
public enum JobStatus
{
    /// <summary>Pronto para ser pego pelo scheduler na próxima janela.</summary>
    Pendente,

    /// <summary>Reservado pelo scheduler (lock obtido) e em execução.</summary>
    Executando,

    /// <summary>One-shot encerrado com sucesso. Recorrentes nunca chegam aqui — voltam para Pendente.</summary>
    Concluido,

    /// <summary>Esgotou as tentativas de retry e parou de ser tentado.</summary>
    Falhou
}
