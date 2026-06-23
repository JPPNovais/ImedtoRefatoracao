using System.Text.Json;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Financeiro;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Application.Admin.CategoriasFinanceiras;

/// <summary>
/// Inativa a categoria padrão global e reflete nas cópias Padrao=true dos estabelecimentos
/// com mesmo nome+tipo (R4 — briefing 2026-06-22_003 M3).
/// </summary>
public class InativarCategoriaFinanceiraPadraoSistemaCommandHandler
{
    private readonly ICategoriaFinanceiraPadraoSistemaRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public InativarCategoriaFinanceiraPadraoSistemaCommandHandler(
        ICategoriaFinanceiraPadraoSistemaRepository repo,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _repo = repo;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(InativarCategoriaFinanceiraPadraoSistemaCommand command, CancellationToken ct = default)
    {
        var global = await _repo.ObterPorIdOuNulo(command.Id)
            ?? throw new BusinessException("Categoria não encontrada.");

        global.Inativar();
        _db.CategoriasFinanceirasPadraoSistema.Update(global);

        // R4 — refletir nas cópias Padrao=true de mesmo nome+tipo nos estabelecimentos
        var copias = await _db.CategoriasFinanceiras
            .Where(c => c.Nome == global.Nome && c.Tipo == global.Tipo && c.Padrao && c.Ativo)
            .ToListAsync(ct);

        foreach (var copia in copias)
            copia.Inativar(); // M5 liberou Inativar() para Padrao=true

        await _db.SaveChangesAsync(ct);

        var payloadJson = JsonSerializer.Serialize(new
        {
            nome = global.Nome,
            tipo = global.Tipo.ToString(),
            nInstanciasPropagadas = copias.Count,
        });

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.InativarCategoriaFinanceiraPadraoSistema,
            command.AdminId,
            recursoTipo: "categoria_financeira_padrao",
            recursoId: global.Id.ToString(),
            payloadJson: payloadJson,
            ct: ct);
    }
}

public record InativarCategoriaFinanceiraPadraoSistemaCommand(long Id, Guid? AdminId);
