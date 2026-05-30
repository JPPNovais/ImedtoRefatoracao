using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Imedto.Backend.Contracts.Admin.Configs.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Configs;

/// <summary>
/// Atualiza uma configuração global existente.
/// Valida tipo antes de persistir (R7).
/// Invalida o cache do ConfigGlobalReader (R8).
/// Grava audit obrigatório (R5, R16).
/// </summary>
public class AtualizarConfigAdminCommandHandler
{
    private readonly AppDbContext _db;
    private readonly IConfigGlobalReader _configReader;
    private readonly ImedtoAdminAuditWriter _audit;

    public AtualizarConfigAdminCommandHandler(
        AppDbContext db,
        IConfigGlobalReader configReader,
        ImedtoAdminAuditWriter audit)
    {
        _db = db;
        _configReader = configReader;
        _audit = audit;
    }

    public async Task Handle(AtualizarConfigAdminCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo deve ter ao menos 10 caracteres.");

        var config = await _db.ImedtoConfigs.FindAsync([command.Chave], ct)
            ?? throw new BusinessException("Configuração não encontrada.");

        ValidarValorPorTipo(config.Tipo, command.Valor);

        var valorAnterior = config.Valor;

        // Armazena como JSON string (campo valor é JSONB)
        var valorJson = ToJson(command.Valor, config.Tipo);
        config.Atualizar(valorJson, command.AdminId);

        await _db.SaveChangesAsync(ct);

        _configReader.InvalidarCache(command.Chave);

        var payload = JsonSerializer.Serialize(new { antes = valorAnterior, depois = valorJson });
        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AtualizarConfig,
            command.AdminId,
            recursoTipo: "config",
            recursoId: command.Chave,
            motivo: command.Motivo,
            payloadJson: payload,
            ct: ct);
    }

    private static string ToJson(string valor, string tipo)
    {
        return tipo.ToLowerInvariant() switch
        {
            "numerico" => valor.Trim(),
            "toggle"   => valor.Trim().ToLowerInvariant() is "true" or "1" ? "true" : "false",
            _          => $"\"{valor.Replace("\"", "\\\"")}\"",
        };
    }

    private static void ValidarValorPorTipo(string tipo, string valor)
    {
        switch (tipo.ToLowerInvariant())
        {
            case "numerico":
                if (!int.TryParse(valor.Trim(), out var n) || n < 0)
                    throw new BusinessException("Valor inválido para chave numérica. Informe um número inteiro positivo.");
                break;

            case "email":
                if (!Regex.IsMatch(valor.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    throw new BusinessException("Valor inválido para chave de e-mail. Informe um endereço de e-mail válido.");
                break;

            case "toggle":
                var v = valor.Trim().ToLowerInvariant();
                if (v is not ("true" or "false" or "1" or "0"))
                    throw new BusinessException("Valor inválido para chave booleana. Informe 'true' ou 'false'.");
                break;
        }
    }
}
