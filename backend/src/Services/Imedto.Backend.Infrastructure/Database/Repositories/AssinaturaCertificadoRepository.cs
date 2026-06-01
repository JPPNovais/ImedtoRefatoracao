using Imedto.Backend.Domain.AssinaturaDigital;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

/// <summary>
/// Repositório de escrita do certificado de assinatura digital.
/// O refresh_token é sempre tratado como opaco aqui (cifrado pelo handler).
/// </summary>
public class AssinaturaCertificadoRepository : IAssinaturaCertificadoRepository
{
    private readonly AppDbContext _context;

    public AssinaturaCertificadoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AssinaturaCertificado?> ObterPorMedicoAsync(Guid medicoId, CancellationToken ct = default) =>
        await _context.AssinaturaCertificados
            .FirstOrDefaultAsync(c => c.MedicoId == medicoId, ct);

    public async Task Salvar(AssinaturaCertificado certificado)
    {
        var existente = await _context.AssinaturaCertificados
            .AnyAsync(c => c.Id == certificado.Id);

        if (!existente)
            await _context.AssinaturaCertificados.AddAsync(certificado);
        else
            _context.AssinaturaCertificados.Update(certificado);
    }

    public Task Remover(AssinaturaCertificado certificado)
    {
        _context.AssinaturaCertificados.Remove(certificado);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Repositório append-only de audit de assinatura digital.
/// </summary>
public class AssinaturaAuditLogRepository : IAssinaturaAuditLogRepository
{
    private readonly AppDbContext _context;

    public AssinaturaAuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SalvarAsync(AssinaturaAuditLog log, CancellationToken ct = default)
    {
        await _context.AssinaturaAuditLogs.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);
    }
}
