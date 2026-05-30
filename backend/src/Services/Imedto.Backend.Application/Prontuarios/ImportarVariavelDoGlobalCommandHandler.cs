using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios;

/// <summary>
/// Importa uma variável pool global para o estabelecimento atual.
/// Cria cópia independente (W2-CA28).
///
/// Mapeamento de tipo: a variável global usa tipos genéricos (texto/numerico/lista/etc).
/// Como o ProntuarioVariavelPool do tenant usa TipoVariavelPool (enum de categoria clínica),
/// o nome da variável importada mantém o contexto. O tipo é mapeado: todos os globais
/// importam como TipoVariavelPool.Medicamento por default — o usuário ajusta depois.
/// Nota: este é o mapeamento conservador; extensão futura pode exigir mapeamento explícito.
/// </summary>
public class ImportarVariavelDoGlobalCommandHandler
{
    private readonly ImedtoVariavelPoolGlobalRepository _globalRepo;
    private readonly IProntuarioVariavelPoolRepository _tenantRepo;

    public ImportarVariavelDoGlobalCommandHandler(
        ImedtoVariavelPoolGlobalRepository globalRepo,
        IProntuarioVariavelPoolRepository tenantRepo)
    {
        _globalRepo = globalRepo;
        _tenantRepo = tenantRepo;
    }

    public async Task<long> Handle(ImportarVariavelDoGlobalCommand command, CancellationToken ct = default)
    {
        var global = await _globalRepo.ObterPorIdAsync(command.IdGlobal, ct)
            ?? throw new BusinessException("Variável global não encontrada.");

        if (!global.Ativo)
            throw new BusinessException("Variável global inativa não pode ser importada.");

        // Mapeamento best-effort: tipo global → TipoVariavelPool tenant.
        // Usuário pode ajustar o tipo depois da importação.
        var tipoTenant = MapearTipo(global.Tipo);

        var copia = ProntuarioVariavelPool.CriarDoEstabelecimento(
            command.EstabelecimentoId,
            tipoTenant,
            global.Nome);

        await _tenantRepo.Salvar(copia);

        return copia.Id;
    }

    private static TipoVariavelPool MapearTipo(string tipoGlobal) => tipoGlobal.ToLowerInvariant() switch
    {
        "lista"    => TipoVariavelPool.Medicamento,
        "texto"    => TipoVariavelPool.Doenca,
        "numerico" => TipoVariavelPool.Doenca,
        "data"     => TipoVariavelPool.Doenca,
        "booleano" => TipoVariavelPool.Doenca,
        _          => TipoVariavelPool.Doenca,
    };
}

public record ImportarVariavelDoGlobalCommand(Guid IdGlobal, long EstabelecimentoId);
