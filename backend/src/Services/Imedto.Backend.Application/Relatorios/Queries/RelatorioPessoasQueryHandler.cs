using Imedto.Backend.Contracts.Relatorios.Queries;
using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Relatorios.Queries;

/// <summary>
/// Handler do relatório de pessoas (pacientes ou profissionais). LGPD: o sub-tipo
/// <c>pacientes</c> só expõe id + nome no top 10 — nunca CPF, telefone, e-mail ou
/// data de nascimento.
/// </summary>
public class RelatorioPessoasQueryHandler : IRequestHandler<RelatorioPessoasQuery, RelatorioPessoasDto>
{
    private readonly RelatorioQueryRepository _repo;

    public RelatorioPessoasQueryHandler(RelatorioQueryRepository repo) => _repo = repo;

    public async Task<RelatorioPessoasDto> Handle(RelatorioPessoasQuery query)
    {
        FiltrosRelatorio.Validar(query.DataInicio, query.DataFim);

        var tipo = (query.Tipo ?? string.Empty).Trim().ToLowerInvariant();
        return tipo switch
        {
            "pacientes" => new RelatorioPessoasDto
            {
                Tipo = tipo,
                Pacientes = await _repo.RelatorioPacientes(query.EstabelecimentoId, query.DataInicio, query.DataFim)
            },
            "profissionais" => new RelatorioPessoasDto
            {
                Tipo = tipo,
                Profissionais = await _repo.RelatorioProfissionais(query.EstabelecimentoId, query.DataInicio, query.DataFim)
            },
            _ => throw new BusinessException("Tipo inválido. Use: pacientes ou profissionais.")
        };
    }
}
