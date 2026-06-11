using Imedto.Backend.Contracts.Relatorios.Queries;
using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Relatorios.Queries;

/// <summary>
/// Handler do relatório financeiro consolidado. Valida filtros (intervalo + enum de
/// agrupamento) e delega o SQL ao <see cref="RelatorioQueryRepository"/>.
/// </summary>
public class RelatorioFinanceiroQueryHandler : IRequestHandler<RelatorioFinanceiroQuery, RelatorioFinanceiroDto>
{
    private static readonly HashSet<string> AgrupamentosValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "dia", "semana", "mes", "categoria", "forma_pagamento"
    };

    private readonly RelatorioQueryRepository _repo;

    public RelatorioFinanceiroQueryHandler(RelatorioQueryRepository repo) => _repo = repo;

    public Task<RelatorioFinanceiroDto> Handle(RelatorioFinanceiroQuery query)
    {
        FiltrosRelatorio.Validar(query.DataInicio, query.DataFim);

        var agrupamento = (query.AgruparPor ?? string.Empty).Trim().ToLowerInvariant();
        if (!AgrupamentosValidos.Contains(agrupamento))
            throw new BusinessException("Agrupamento inválido. Use: dia, semana, mes, categoria ou forma_pagamento.");

        return _repo.RelatorioFinanceiro(query.EstabelecimentoId, query.DataInicio, query.DataFim, agrupamento, query.IncluirPorPaciente);
    }
}
