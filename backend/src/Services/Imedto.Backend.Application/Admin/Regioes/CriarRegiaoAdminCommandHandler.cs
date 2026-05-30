using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Catalogo;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.Infrastructure.Admin.QueryRepositories;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Regioes;

public class CriarRegiaoAdminCommandHandler
{
    private readonly RegiaoAnatomicaCatalogoRepository _repo;
    private readonly RegiaoAnatomicaAdminQueryRepository _query;
    private readonly ImedtoAdminAuditWriter _audit;

    public CriarRegiaoAdminCommandHandler(
        RegiaoAnatomicaCatalogoRepository repo,
        RegiaoAnatomicaAdminQueryRepository query,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _query = query;
        _audit = audit;
    }

    public async Task<long> Handle(CriarRegiaoAdminCommand command, CancellationToken ct = default)
    {
        if (command.Motivo.Trim().Length < 10)
            throw new BusinessException("Informe o motivo da alteração (mínimo 10 caracteres).");

        if (await _query.ExisteCodigoAsync(command.Codigo, ct: ct))
            throw new BusinessException("Já existe região anatômica com esse código.");

        // Valida pai quando informado (W4-CA16)
        if (!string.IsNullOrWhiteSpace(command.PaiCodigo))
        {
            var pai = await _repo.ObterPorCodigoOuNulo(command.PaiCodigo);
            if (pai is null)
                throw new BusinessException("Código do pai não encontrado.");
            if (command.Nivel != pai.Nivel + 1)
                throw new BusinessException("Nível inconsistente com pai.");
            if (!string.Equals(command.Vista, pai.Vista, StringComparison.Ordinal))
                throw new BusinessException("Vista deve ser igual à do pai.");
        }

        var regiao = RegiaoAnatomicaCatalogo.Criar(
            command.Codigo,
            command.Nome,
            string.IsNullOrWhiteSpace(command.PaiCodigo) ? null : command.PaiCodigo,
            command.Nivel,
            string.IsNullOrWhiteSpace(command.Vista) ? null : command.Vista,
            string.IsNullOrWhiteSpace(command.TemplateTexto) ? null : command.TemplateTexto,
            null,
            command.Ordem,
            command.Lateralidade);

        await _repo.Adicionar(regiao);

        await _audit.RegistrarAsync(
            AcoesAuditAdmin.CriarRegiaoAnatomica,
            command.AdminId,
            recursoTipo: "regiao_anatomica",
            recursoId: regiao.Id.ToString(),
            motivo: command.Motivo,
            ct: ct);

        return regiao.Id;
    }
}

public record CriarRegiaoAdminCommand(
    string Codigo,
    string Nome,
    string? PaiCodigo,
    short Nivel,
    string? Vista,
    string? TemplateTexto,
    short Ordem,
    bool Lateralidade,
    string Motivo,
    Guid? AdminId);
