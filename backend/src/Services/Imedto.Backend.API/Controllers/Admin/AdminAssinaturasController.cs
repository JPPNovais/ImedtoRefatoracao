using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.Application.Admin.Assinaturas;
using Imedto.Backend.Contracts.Admin.Assinaturas.Commands;
using Imedto.Backend.Contracts.Admin.Assinaturas.Queries;
using Imedto.Backend.Contracts.Admin.Assinaturas.Queries.Results;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers.Admin;

/// <summary>
/// Gerenciamento de assinaturas por estabelecimento (admin global).
/// Histórico imutável: toda troca de plano é INSERT + FecharVigencia, nunca UPDATE in-place.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = "ImedtoAdmin")]
public class AdminAssinaturasController : ControllerBase
{
    private readonly ListarHistoricoAssinaturasAdminQueryHandler _listarHistorico;
    private readonly TrocarPlanoAdminCommandHandler _trocarPlano;
    private readonly ConcederGratuidadeAdminCommandHandler _concederGratuidade;
    private readonly EncerrarAssinaturaAdminCommandHandler _encerrar;

    public AdminAssinaturasController(
        ListarHistoricoAssinaturasAdminQueryHandler listarHistorico,
        TrocarPlanoAdminCommandHandler trocarPlano,
        ConcederGratuidadeAdminCommandHandler concederGratuidade,
        EncerrarAssinaturaAdminCommandHandler encerrar)
    {
        _listarHistorico = listarHistorico;
        _trocarPlano = trocarPlano;
        _concederGratuidade = concederGratuidade;
        _encerrar = encerrar;
    }

    [HttpGet("api/admin/estabelecimentos/{eid:long}/assinaturas")]
    [ProducesResponseType(typeof(IReadOnlyList<AssinaturaAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarHistorico(long eid, CancellationToken ct = default)
    {
        var result = await _listarHistorico.Handle(new ListarHistoricoAssinaturasAdminQuery(eid), ct);
        return Ok(result);
    }

    [HttpPost("api/admin/estabelecimentos/{eid:long}/assinaturas/trocar-plano")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> TrocarPlano(
        long eid,
        [FromBody] TrocarPlanoAdminRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _trocarPlano.Handle(new TrocarPlanoAdminCommand(
            eid,
            request.PlanoId,
            request.Inicio,
            request.FimEm,
            request.Motivo,
            adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/estabelecimentos/{eid:long}/assinaturas/gratuidade")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConcederGratuidade(
        long eid,
        [FromBody] ConcederGratuidadeAdminRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _concederGratuidade.Handle(new ConcederGratuidadeAdminCommand(
            eid,
            request.GratuidadeMotivo,
            request.FimEm,
            request.Motivo,
            adminId), ct);
        return NoContent();
    }

    [HttpPost("api/admin/assinaturas/{id:guid}/encerrar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Encerrar(
        Guid id,
        [FromBody] EncerrarAssinaturaRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _encerrar.Handle(new EncerrarAssinaturaAdminCommand(id, request.Motivo, adminId), ct);
        return NoContent();
    }

    private Guid ObterAdminId()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var id))
            throw new BusinessException("Sessão de administrador inválida.");
        return id;
    }
}

public record TrocarPlanoAdminRequest(
    Guid PlanoId,
    DateTimeOffset Inicio,
    DateTimeOffset? FimEm,
    string Motivo);

public record ConcederGratuidadeAdminRequest(
    string GratuidadeMotivo,
    DateTimeOffset? FimEm,
    string Motivo);

public record EncerrarAssinaturaRequest(string Motivo);
