using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Infrastructure.Database;
using Microsoft.Extensions.Logging;

namespace Imedto.Backend.Infrastructure.Termos;

/// <summary>
/// Implementação EF Core de <see cref="ITermoAuditLogger"/>. Faz append em
/// <c>termo_audit_log</c>; o save real acontece no commit do UoW (mesma transação
/// do handler que disparou a auditoria — se a operação falhar, o audit cai junto,
/// que é o comportamento desejado para audit consistente).
///
/// Não lança em caso de erro de validação interna — registra warning e segue.
/// </summary>
public sealed class EfTermoAuditLogger : ITermoAuditLogger
{
    private readonly AppDbContext _context;
    private readonly ILogger<EfTermoAuditLogger> _logger;

    public EfTermoAuditLogger(AppDbContext context, ILogger<EfTermoAuditLogger> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RegistrarAsync(
        long? estabelecimentoId,
        Guid? usuarioId,
        string acao,
        string entidade,
        long entidadeId,
        string metadataJson = null,
        string ipOrigem = null,
        CancellationToken ct = default)
    {
        try
        {
            var log = TermoAuditLog.Registrar(estabelecimentoId, usuarioId, acao, entidade, entidadeId, metadataJson, ipOrigem);
            await _context.Set<TermoAuditLog>().AddAsync(log, ct);
        }
        catch (Exception ex)
        {
            // Auditoria não pode quebrar o fluxo principal. Logamos pra investigar depois.
            _logger.LogWarning(ex, "Falha ao gravar audit log de termo (acao={Acao}, entidade={Entidade}, id={EntidadeId})",
                acao, entidade, entidadeId);
        }
    }
}
