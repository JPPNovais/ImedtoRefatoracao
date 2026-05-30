using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios;

/// <summary>
/// Importa um modelo de prontuário global para o estabelecimento atual.
/// Cria uma cópia independente — sem live-link com o original (W2-CA25, W2-CA26, W2-CA27).
/// </summary>
public class ImportarModeloDoGlobalCommandHandler
{
    private readonly ImedtoModeloProntuarioGlobalRepository _globalRepo;
    private readonly IModeloDeProntuarioRepository _tenantRepo;

    public ImportarModeloDoGlobalCommandHandler(
        ImedtoModeloProntuarioGlobalRepository globalRepo,
        IModeloDeProntuarioRepository tenantRepo)
    {
        _globalRepo = globalRepo;
        _tenantRepo = tenantRepo;
    }

    public async Task<long> Handle(ImportarModeloDoGlobalCommand command, CancellationToken ct = default)
    {
        var global = await _globalRepo.ObterPorIdAsync(command.IdGlobal, ct)
            ?? throw new BusinessException("Modelo global não encontrado.");

        if (!global.Ativo)
            throw new BusinessException("Modelo global inativo não pode ser importado.");

        // Cópia independente — usa ConteudoJson como EstruturaJson (schema compatível, briefing §6).
        var copia = ModeloDeProntuario.CriarDoEstabelecimento(
            command.EstabelecimentoId,
            global.Nome,
            global.Descricao ?? string.Empty,
            global.ConteudoJson);

        await _tenantRepo.Salvar(copia);

        return copia.Id;
    }
}

public record ImportarModeloDoGlobalCommand(Guid IdGlobal, long EstabelecimentoId);
