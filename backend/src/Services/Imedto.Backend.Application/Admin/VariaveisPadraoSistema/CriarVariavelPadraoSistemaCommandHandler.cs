using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.VariaveisPadraoSistema;

public class CriarVariavelPadraoSistemaCommandHandler
{
    private readonly IProntuarioVariavelPoolRepository _repo;
    private readonly VariavelPadraoSistemaQueryRepository _query;
    private readonly ImedtoAdminAuditWriter _audit;

    public CriarVariavelPadraoSistemaCommandHandler(
        IProntuarioVariavelPoolRepository repo,
        VariavelPadraoSistemaQueryRepository query,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _query = query;
        _audit = audit;
    }

    public async Task<long> Handle(CriarVariavelPadraoSistemaCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da alteração (mínimo 10 caracteres).");

        if (!Enum.TryParse<TipoVariavelPool>(command.Tipo, ignoreCase: true, out var tipo))
            throw new BusinessException("Categoria inválida.");

        if (await _query.ExisteNomePorCategoriaParaSistema(command.Nome, command.Tipo, ct: ct))
            throw new BusinessException("Já existe variável padrão do sistema com esse nome nessa categoria.");

        var variavel = ProntuarioVariavelPool.CriarPadraoSistema(tipo, command.Nome);

        await _repo.Salvar(variavel);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.CriarVariavelPadraoSistema,
            command.AdminId,
            recursoTipo: "variavel_pool",
            recursoId: variavel.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);

        return variavel.Id;
    }
}

public record CriarVariavelPadraoSistemaCommand(
    string Nome,
    string Tipo,
    string Motivo,
    Guid? AdminId);
