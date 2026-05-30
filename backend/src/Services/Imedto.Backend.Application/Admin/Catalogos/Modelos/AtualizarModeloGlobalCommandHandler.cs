using System.Text.Json;
using Imedto.Backend.Contracts.Admin.Catalogos.Modelos.Commands;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Catalogos.Modelos;

public class AtualizarModeloGlobalCommandHandler
{
    private readonly ImedtoModeloProntuarioGlobalRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public AtualizarModeloGlobalCommandHandler(
        ImedtoModeloProntuarioGlobalRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(AtualizarModeloGlobalCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo deve ter ao menos 10 caracteres.");

        var modelo = await _repo.ObterPorIdAsync(command.Id, ct)
            ?? throw new BusinessException("Modelo não encontrado.");

        var nomeAnterior = modelo.Nome;
        modelo.Atualizar(command.Nome, command.Descricao, command.ConteudoJson, command.AdminId);
        await _repo.SalvarAsync(ct);

        var payload = JsonSerializer.Serialize(new { nome_antes = nomeAnterior, nome_depois = command.Nome });
        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AtualizarModeloGlobal,
            command.AdminId,
            recursoTipo: "modelo_prontuario_global",
            recursoId: command.Id.ToString(),
            motivo: command.Motivo,
            payloadJson: payload,
            ct: ct);
    }
}
