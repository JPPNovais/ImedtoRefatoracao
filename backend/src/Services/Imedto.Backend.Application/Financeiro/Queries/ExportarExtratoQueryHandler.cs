using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Financeiro.Queries;

/// <summary>
/// Handler singleton da ExportarExtratoQuery.
/// Responsabilidades:
///   1. Delega a query de leitura ao ConsolidacaoFinanceiraQueryRepository.
///   2. Grava audit LGPD via repositório (CA10 — sem PII no log).
///   Falha de audit é best-effort: nunca bloqueia o export.
/// </summary>
public class ExportarExtratoQueryHandler : IRequestHandler<ExportarExtratoQuery, ExportarExtratoResultDto>
{
    private readonly ConsolidacaoFinanceiraQueryRepository _repo;

    public ExportarExtratoQueryHandler(ConsolidacaoFinanceiraQueryRepository repo)
        => _repo = repo;

    public async Task<ExportarExtratoResultDto> Handle(ExportarExtratoQuery query)
    {
        if (query.EstabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento não identificado.");
        if (query.DataInicio > query.DataFim)
            throw new BusinessException("Data de início não pode ser posterior à data de fim.");

        var itens = await _repo.ExportarExtrato(
            query.EstabelecimentoId,
            query.DataInicio,
            query.DataFim,
            query.Tipo,
            query.Categoria,
            query.FormaPagamento,
            query.Origem);

        // Audit LGPD: best-effort via repositório (CA10).
        // Falha de audit nunca bloqueia o export — engole exceção aqui.
        try
        {
            await _repo.GravarExportAuditAsync(
                query.UsuarioId, query.EstabelecimentoId,
                query.DataInicio, query.DataFim, itens.Count);
        }
        catch
        {
            // best-effort: o repositório também engole, mas garantimos aqui por completude.
        }

        return new ExportarExtratoResultDto
        {
            Itens = itens,
            TotalLinhas = itens.Count,
            DataInicio = query.DataInicio,
            DataFim = query.DataFim
        };
    }
}
