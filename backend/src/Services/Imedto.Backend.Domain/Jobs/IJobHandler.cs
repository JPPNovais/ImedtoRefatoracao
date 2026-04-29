namespace Imedto.Backend.Domain.Jobs;

/// <summary>
/// Implementação concreta de um job rodado pelo scheduler.
/// O <see cref="Nome"/> deve bater com a coluna <c>nome</c> da tabela <c>jobs_agendados</c> e é único.
/// O scheduler resolve handlers por nome dentro de um scope DI próprio por execução.
/// </summary>
public interface IJobHandler
{
    /// <summary>Identificador único e estável do job (ex: <c>"limpar-audit-antigo"</c>).</summary>
    string Nome { get; }

    /// <summary>
    /// Lógica do job. Não deve engolir <see cref="OperationCanceledException"/> —
    /// o scheduler trata cancelamento separadamente do erro de negócio.
    /// </summary>
    Task ExecutarAsync(CancellationToken ct);
}
