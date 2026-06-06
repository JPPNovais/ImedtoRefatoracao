using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

public class AdicionarVariavelPoolCommandHandler : ICommandHandler<AdicionarVariavelPoolCommand>
{
    private readonly IProntuarioVariavelPoolRepository _repository;

    public AdicionarVariavelPoolCommandHandler(IProntuarioVariavelPoolRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AdicionarVariavelPoolCommand command)
    {
        if (!Enum.TryParse<TipoVariavelPool>(command.Tipo, ignoreCase: true, out var tipo))
            throw new BusinessException("Tipo inválido. Use: Alergia, Medicamento, Doenca, Cirurgia, RelacaoFamiliar, Expectativa.");

        if (await _repository.ExisteOutraComMesmoNome(command.EstabelecimentoId, tipo, command.Nome ?? string.Empty, 0))
            throw new BusinessException("Já existe uma opção com esse nome para esta lista.");

        var item = ProntuarioVariavelPool.CriarDoEstabelecimento(command.EstabelecimentoId, tipo, command.Nome);
        await _repository.Salvar(item);
    }
}
