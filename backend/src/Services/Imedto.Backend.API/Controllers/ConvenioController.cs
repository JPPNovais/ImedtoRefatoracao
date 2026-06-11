using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Convenios.Commands;
using Imedto.Backend.Contracts.Convenios.Queries;
using Imedto.Backend.Contracts.Convenios.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/convenios")]
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]
public class ConvenioController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;
    // Repositório Dapper para select rápido (endpoint de pré-seleção check-in).
    private readonly ConvenioQueryRepository _convenioQueryRepo;

    public ConvenioController(
        ICommandBus cmd,
        IRequestBus query,
        ICurrentTenantAccessor tenant,
        ConvenioQueryRepository convenioQueryRepo)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
        _convenioQueryRepo = convenioQueryRepo;
    }

    /// <summary>Lista convênios do estabelecimento. RBAC: convenios.ver.</summary>
    [HttpGet]
    [RequiresAcao("convenios", "ver")]
    public async Task<ActionResult<IReadOnlyList<ConvenioListadoDto>>> Listar([FromQuery] bool apenasAtivos = false)
    {
        var result = await _query.Query<ListarConveniosQuery, IReadOnlyList<ConvenioListadoDto>>(
            new ListarConveniosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                ApenasAtivos = apenasAtivos,
            });
        return Ok(result);
    }

    /// <summary>Retorna detalhes + planos de um convênio. RBAC: convenios.ver.</summary>
    [HttpGet("{id:long}")]
    [RequiresAcao("convenios", "ver")]
    public async Task<ActionResult<ConvenioDetalheDto>> Obter(long id)
    {
        var result = await _query.Query<ObterConvenioQuery, ConvenioDetalheDto?>(
            new ObterConvenioQuery { ConvenioId = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>Retorna convênios ativos com planos para selects do check-in/carteirinha. RBAC: convenios.ver.</summary>
    [HttpGet("ativos")]
    [RequiresAcao("convenios", "ver")]
    public async Task<ActionResult<IReadOnlyList<ConvenioSelectDto>>> ListarAtivos()
    {
        var result = await _convenioQueryRepo.ListarAtivosComPlanos(_tenant.EstabelecimentoId);
        return Ok(result);
    }

    /// <summary>Cria novo convênio. RBAC: convenios.gerenciar.</summary>
    [HttpPost]
    [RequiresAcao("convenios", "gerenciar")]
    public async Task<ActionResult> Criar([FromBody] CriarConvenioRequest dto)
    {
        await _cmd.Send(new CriarConvenioCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            Nome = dto.Nome,
            RegistroAns = dto.RegistroAns,
        });
        return Ok();
    }

    /// <summary>Atualiza convênio (nome, ANS, ativo). RBAC: convenios.gerenciar.</summary>
    [HttpPut("{id:long}")]
    [RequiresAcao("convenios", "gerenciar")]
    public async Task<ActionResult> Atualizar(long id, [FromBody] AtualizarConvenioRequest dto)
    {
        await _cmd.Send(new AtualizarConvenioCommand
        {
            ConvenioId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            Nome = dto.Nome,
            RegistroAns = dto.RegistroAns,
            Ativo = dto.Ativo,
        });
        return Ok();
    }

    /// <summary>Exclui fisicamente (apenas se sem uso). RBAC: convenios.gerenciar.</summary>
    [HttpDelete("{id:long}")]
    [RequiresAcao("convenios", "gerenciar")]
    public async Task<ActionResult> Excluir(long id)
    {
        await _cmd.Send(new ExcluirConvenioCommand
        {
            ConvenioId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
        });
        return Ok();
    }

    // ── Planos ────────────────────────────────────────────────────────────────

    /// <summary>Adiciona plano ao convênio. RBAC: convenios.gerenciar.</summary>
    [HttpPost("{convenioId:long}/planos")]
    [RequiresAcao("convenios", "gerenciar")]
    public async Task<ActionResult> AdicionarPlano(long convenioId, [FromBody] PlanoNomeRequest dto)
    {
        await _cmd.Send(new AdicionarPlanoConvenioCommand
        {
            ConvenioId = convenioId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            Nome = dto.Nome,
        });
        return Ok();
    }

    /// <summary>Atualiza nome do plano. RBAC: convenios.gerenciar.</summary>
    [HttpPut("{convenioId:long}/planos/{planoId:long}")]
    [RequiresAcao("convenios", "gerenciar")]
    public async Task<ActionResult> AtualizarPlano(long convenioId, long planoId, [FromBody] PlanoNomeRequest dto)
    {
        await _cmd.Send(new AtualizarPlanoConvenioCommand
        {
            ConvenioId = convenioId,
            PlanoId = planoId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            Nome = dto.Nome,
        });
        return Ok();
    }

    /// <summary>Inativa plano. RBAC: convenios.gerenciar.</summary>
    [HttpDelete("{convenioId:long}/planos/{planoId:long}")]
    [RequiresAcao("convenios", "gerenciar")]
    public async Task<ActionResult> InativarPlano(long convenioId, long planoId)
    {
        await _cmd.Send(new InativarPlanoConvenioCommand
        {
            ConvenioId = convenioId,
            PlanoId = planoId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
        });
        return Ok();
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────
public record CriarConvenioRequest(string Nome, string? RegistroAns);
public record AtualizarConvenioRequest(string Nome, string? RegistroAns, bool Ativo);
public record PlanoNomeRequest(string Nome);
