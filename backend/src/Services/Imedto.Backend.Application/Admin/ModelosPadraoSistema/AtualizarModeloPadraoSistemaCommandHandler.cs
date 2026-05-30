using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.Infrastructure.Database;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.ModelosPadraoSistema;

public class AtualizarModeloPadraoSistemaCommandHandler
{
    private readonly IModeloDeProntuarioRepository _repo;
    private readonly ModeloPadraoSistemaQueryRepository _query;
    private readonly ImedtoAdminAuditWriter _audit;
    private readonly AppDbContext _db;

    public AtualizarModeloPadraoSistemaCommandHandler(
        IModeloDeProntuarioRepository repo,
        ModeloPadraoSistemaQueryRepository query,
        ImedtoAdminAuditWriter audit,
        AppDbContext db)
    {
        _repo = repo;
        _query = query;
        _audit = audit;
        _db = db;
    }

    public async Task Handle(AtualizarModeloPadraoSistemaCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da alteração (mínimo 10 caracteres).");

        var modelo = await _repo.ObterVisivelOuNulo(command.Id, 0);
        if (modelo is null || !modelo.EhPadraoSistema)
            throw new BusinessException("Modelo não encontrado.");

        if (await _query.ExisteNomeParaSistema(command.Nome, ignorarId: command.Id, ct: ct))
            throw new BusinessException("Já existe modelo padrão do sistema com esse nome.");

        modelo.AtualizarDados(command.Nome, command.Descricao, command.EstruturaJson);
        _db.ModelosDeProntuario.Update(modelo);
        await _db.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.AtualizarModeloPadraoSistema,
            command.AdminId,
            recursoTipo: "modelo_prontuario",
            recursoId: modelo.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);
    }
}

public record AtualizarModeloPadraoSistemaCommand(
    long Id,
    string Nome,
    string? Descricao,
    string EstruturaJson,
    string Motivo,
    Guid? AdminId);
