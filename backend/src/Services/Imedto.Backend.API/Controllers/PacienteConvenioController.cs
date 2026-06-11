using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.PacienteConvenios.Commands;
using Imedto.Backend.Contracts.PacienteConvenios.Queries;
using Imedto.Backend.Contracts.PacienteConvenios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/pacientes/{pacienteId:long}/convenios")]
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]
public class PacienteConvenioController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public PacienteConvenioController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
    }

    /// <summary>Lista carteirinhas do paciente. RBAC: pacientes.ver (R12/CA140).</summary>
    [HttpGet]
    [RequiresAcao("pacientes", "ver")]
    public async Task<ActionResult<IReadOnlyList<PacienteConvenioDto>>> Listar(long pacienteId)
    {
        var result = await _query.Query<ListarPacienteConveniosQuery, IReadOnlyList<PacienteConvenioDto>>(
            new ListarPacienteConveniosQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                UsuarioSolicitanteId = _tenant.UsuarioId,
            });
        return Ok(result);
    }

    /// <summary>Adiciona carteirinha ao paciente. RBAC: pacientes.editar (R12/CA140).</summary>
    [HttpPost]
    [RequiresAcao("pacientes", "editar")]
    public async Task<ActionResult> Criar(long pacienteId, [FromBody] CriarCarteirinhaRequest dto)
    {
        await _cmd.Send(new CriarPacienteConvenioCommand
        {
            PacienteId = pacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            ConvenioId = dto.ConvenioId,
            PlanoId = dto.PlanoId,
            NumeroCarteirinha = dto.NumeroCarteirinha,
            Validade = dto.Validade,
        });
        return Ok();
    }

    /// <summary>Atualiza carteirinha. RBAC: pacientes.editar.</summary>
    [HttpPut("{carteirinhaId:long}")]
    [RequiresAcao("pacientes", "editar")]
    public async Task<ActionResult> Atualizar(long pacienteId, long carteirinhaId, [FromBody] AtualizarCarteirinhaRequest dto)
    {
        await _cmd.Send(new AtualizarPacienteConvenioCommand
        {
            CarteirinhaId = carteirinhaId,
            PacienteId = pacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
            ConvenioId = dto.ConvenioId,
            PlanoId = dto.PlanoId,
            NumeroCarteirinha = dto.NumeroCarteirinha,
            Validade = dto.Validade,
            Ativo = dto.Ativo,
        });
        return Ok();
    }

    /// <summary>Remove carteirinha. RBAC: pacientes.editar.</summary>
    [HttpDelete("{carteirinhaId:long}")]
    [RequiresAcao("pacientes", "editar")]
    public async Task<ActionResult> Excluir(long pacienteId, long carteirinhaId)
    {
        await _cmd.Send(new ExcluirPacienteConvenioCommand
        {
            CarteirinhaId = carteirinhaId,
            PacienteId = pacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioSolicitanteId = _tenant.UsuarioId,
        });
        return Ok();
    }

    /// <summary>
    /// Retorna carteirinhas ativas para pré-seleção no check-in (R8/CA143).
    /// RBAC: pacientes.ver. Não grava audit (helper do fluxo de check-in, não acesso ao prontuário).
    /// </summary>
    [HttpGet("check-in")]
    [RequiresAcao("pacientes", "ver")]
    public async Task<ActionResult<IReadOnlyList<CarteirinhaCheckInDto>>> ObterParaCheckIn(long pacienteId)
    {
        var result = await _query.Query<ObterCarteirinhaAtivaCheckInQuery, IReadOnlyList<CarteirinhaCheckInDto>>(
            new ObterCarteirinhaAtivaCheckInQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
            });
        return Ok(result);
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────
public record CriarCarteirinhaRequest(
    long ConvenioId,
    long? PlanoId,
    string NumeroCarteirinha,
    DateOnly? Validade);

public record AtualizarCarteirinhaRequest(
    long ConvenioId,
    long? PlanoId,
    string NumeroCarteirinha,
    DateOnly? Validade,
    bool Ativo);
