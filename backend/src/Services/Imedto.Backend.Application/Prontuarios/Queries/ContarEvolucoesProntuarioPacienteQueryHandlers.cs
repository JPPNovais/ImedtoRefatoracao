using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Prontuarios.Queries;

/// <summary>
/// Apenas COUNT — não há leitura de conteúdo clínico, então não registra audit log
/// (audit é para leitura efetiva do prontuário/evolução).
/// </summary>
public class ContarEvolucoesProntuarioPacienteQueryHandlers
    : IRequestHandler<ContarEvolucoesProntuarioPacienteQuery, ContagemEvolucoesDto>
{
    private readonly ProntuarioQueryRepository _queryRepository;

    public ContarEvolucoesProntuarioPacienteQueryHandlers(ProntuarioQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<ContagemEvolucoesDto> Handle(ContarEvolucoesProntuarioPacienteQuery query)
    {
        var total = await _queryRepository.ContarEvolucoes(
            query.PacienteId,
            query.EstabelecimentoId,
            query.SolicitanteUsuarioId,
            query.SolicitantePapel);
        return new ContagemEvolucoesDto(total);
    }
}
