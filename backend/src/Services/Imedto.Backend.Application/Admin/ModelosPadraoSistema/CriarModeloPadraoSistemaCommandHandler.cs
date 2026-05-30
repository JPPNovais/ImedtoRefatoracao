using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.ModelosPadraoSistema;

public class CriarModeloPadraoSistemaCommandHandler
{
    private readonly IModeloDeProntuarioRepository _repo;
    private readonly ModeloPadraoSistemaQueryRepository _query;
    private readonly ImedtoAdminAuditWriter _audit;

    public CriarModeloPadraoSistemaCommandHandler(
        IModeloDeProntuarioRepository repo,
        ModeloPadraoSistemaQueryRepository query,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _query = query;
        _audit = audit;
    }

    public async Task<long> Handle(CriarModeloPadraoSistemaCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da alteração (mínimo 10 caracteres).");

        if (await _query.ExisteNomeParaSistema(command.Nome, ct: ct))
            throw new BusinessException("Já existe modelo padrão do sistema com esse nome.");

        var modelo = ModeloDeProntuario.CriarPadraoSistema(
            command.Nome,
            command.Descricao,
            command.EstruturaJson);

        await _repo.Salvar(modelo);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.CriarModeloPadraoSistema,
            command.AdminId,
            recursoTipo: "modelo_prontuario",
            recursoId: modelo.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);

        return modelo.Id;
    }
}

public record CriarModeloPadraoSistemaCommand(
    string Nome,
    string? Descricao,
    string EstruturaJson,
    string Motivo,
    Guid? AdminId);
