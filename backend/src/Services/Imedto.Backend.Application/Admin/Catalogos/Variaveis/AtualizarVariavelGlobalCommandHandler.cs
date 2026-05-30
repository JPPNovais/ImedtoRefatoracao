using System.Text.Json;
using Imedto.Backend.Contracts.Admin.Catalogos.Variaveis.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Catalogos.Variaveis;

public class AtualizarVariavelGlobalCommandHandler
{
    private readonly ImedtoVariavelPoolGlobalRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public AtualizarVariavelGlobalCommandHandler(
        ImedtoVariavelPoolGlobalRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(AtualizarVariavelGlobalCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo deve ter ao menos 10 caracteres.");

        var variavel = await _repo.ObterPorIdAsync(command.Id, ct)
            ?? throw new BusinessException("Variável não encontrada.");

        var nomeAnterior = variavel.Nome;
        variavel.Atualizar(command.Nome, command.Tipo, command.ValoresJson, command.Descricao, command.AdminId);
        await _repo.SalvarAsync(ct);

        var payload = JsonSerializer.Serialize(new { nome_antes = nomeAnterior, nome_depois = command.Nome });
        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AtualizarVariavelGlobal,
            command.AdminId,
            recursoTipo: "variavel_pool_global",
            recursoId: command.Id.ToString(),
            motivo: command.Motivo,
            payloadJson: payload,
            ct: ct);
    }
}
