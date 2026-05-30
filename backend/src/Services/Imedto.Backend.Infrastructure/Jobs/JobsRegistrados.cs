namespace Imedto.Backend.Infrastructure.Jobs;

/// <summary>
/// Registro estático e centralizado dos jobs recorrentes do produto.
/// O <see cref="JobScheduler"/> usa essa lista no bootstrap para garantir que
/// existe linha em <c>jobs_agendados</c> para cada job conhecido — se o handler
/// existe mas não há linha, o scheduler cria com o intervalo abaixo.
///
/// Para adicionar um novo job recorrente:
/// 1. Implementar <see cref="Imedto.Backend.Domain.Jobs.IJobHandler"/>.
/// 2. Registrar o handler como Scoped no <c>Container</c>.
/// 3. Adicionar a entrada aqui com o mesmo <c>Nome</c>.
/// </summary>
public static class JobsRegistrados
{
    public sealed record Entry(string Nome, int IntervaloSeg);

    /// <summary>
    /// Jobs do sistema. <c>IntervaloSeg</c> = 0 sinaliza one-shot — não usado por jobs
    /// dessa lista, que são todos recorrentes. One-shots são criados via comandos de domínio.
    /// </summary>
    public static readonly IReadOnlyList<Entry> Todos = new List<Entry>
    {
        // Limpeza de tentativas de delete antigas (LGPD: retenção de 90 dias). Roda a cada 24h.
        new("limpar-audit-antigo", IntervaloSeg: 24 * 60 * 60),

        // Wave 7 — Retenção do audit admin global (TTL por ação via AuditLogRetencao). Roda 1×/dia.
        new("limpar-audit-admin", IntervaloSeg: 24 * 60 * 60),

        // Item 2.2 — Engine de automações: drena automation_events a cada 30s.
        new("processar-automacoes", IntervaloSeg: 30),

        // Item 2.7 — Expirar trials vencidos. Roda 1x/h; idempotente.
        new("expirar-trials", IntervaloSeg: 60 * 60),

        // Item 3.8 — Limpeza de cache de IA expirado. Roda 1x/h.
        new("limpar-cache-ia", IntervaloSeg: 60 * 60),

        // Item 4.3 — Anonimização LGPD de pacientes com retenção vencida (CFM 1.821/07: 20 anos).
        // Roda mensalmente (~30 dias = 2.592.000 s).
        new("anonimizar-pacientes-inativos", IntervaloSeg: 2592000),
    };
}
