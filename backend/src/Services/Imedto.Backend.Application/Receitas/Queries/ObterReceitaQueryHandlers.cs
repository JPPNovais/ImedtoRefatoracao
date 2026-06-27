using Imedto.Backend.Contracts.Receitas.Queries;
using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Receitas.Queries;

/// <summary>
/// Detalhe da receita (com itens). Audit LGPD obrigatório por se tratar de
/// medicação prescrita — dado clínico sensível.
/// </summary>
public class ObterReceitaQueryHandlers : IRequestHandler<ObterReceitaQuery, ReceitaDto>
{
    private readonly IReceitaQueryRepository _queryRepo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public ObterReceitaQueryHandlers(
        IReceitaQueryRepository queryRepo,
        IProntuarioAcessoLogService acessoLog)
    {
        _queryRepo = queryRepo;
        _acessoLog = acessoLog;
    }

    public async Task<ReceitaDto> Handle(ObterReceitaQuery query)
    {
        var receita = await _queryRepo.ObterCompleta(query.ReceitaId, query.EstabelecimentoId)
            ?? throw new BusinessException("Receita não encontrada.");

        // Gating autor-ou-dono (R1/R5 briefing 2026-06-27_001): mensagem genérica — não vaza autoria.
        var ehDono = query.SolicitantePapel == SharedKernel.Tenancy.TenantPapel.Dono;
        if (!ehDono && receita.ProfissionalUsuarioId != query.SolicitanteUsuarioId)
            throw new BusinessException("Receita não encontrada.");

        await _acessoLog.RegistrarAsync(
            receita.ProntuarioId,
            query.SolicitanteUsuarioId,
            query.EstabelecimentoId,
            TipoAcessoProntuario.Leitura);

        return receita;
    }
}
