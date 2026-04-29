using Imedto.Backend.Contracts.Relatorios.Queries;
using Imedto.Backend.Contracts.Relatorios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Relatorios.Queries;

/// <summary>
/// Handler do relatório operacional. Roteia para uma das 3 sub-rotinas SQL conforme
/// <c>tipo</c> (dashboard/agenda/inventário). Cada sub-tipo preenche apenas a sub-seção
/// correspondente do DTO — as demais ficam <see langword="null"/>.
/// </summary>
public class RelatorioOperacionalQueryHandler : IRequestHandler<RelatorioOperacionalQuery, RelatorioOperacionalDto>
{
    private readonly RelatorioQueryRepository _repo;

    public RelatorioOperacionalQueryHandler(RelatorioQueryRepository repo) => _repo = repo;

    public async Task<RelatorioOperacionalDto> Handle(RelatorioOperacionalQuery query)
    {
        FiltrosRelatorio.Validar(query.DataInicio, query.DataFim);

        var tipo = (query.Tipo ?? string.Empty).Trim().ToLowerInvariant();
        return tipo switch
        {
            "dashboard" => new RelatorioOperacionalDto
            {
                Tipo = tipo,
                Dashboard = await _repo.RelatorioOperacionalDashboard(query.EstabelecimentoId, query.DataInicio, query.DataFim)
            },
            "agenda" => new RelatorioOperacionalDto
            {
                Tipo = tipo,
                Agenda = await _repo.RelatorioOperacionalAgenda(query.EstabelecimentoId, query.DataInicio, query.DataFim)
            },
            "inventario" => new RelatorioOperacionalDto
            {
                Tipo = tipo,
                Inventario = await _repo.RelatorioOperacionalInventario(query.EstabelecimentoId, query.DataInicio, query.DataFim)
            },
            _ => throw new BusinessException("Tipo inválido. Use: dashboard, agenda ou inventario.")
        };
    }
}
