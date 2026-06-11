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
/// Historico imutavel: toda troca de plano é INSERT + FecharVigencia, nunca UPDATE in-place.
/// F4: ações de estado (liberar vitalicio/ate data/trial/suspender/reativar) + cache invalidation.
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
    private readonly LiberarVitalicioAdminCommandHandler _liberarVitalicio;
    private readonly LiberarAteDataAdminCommandHandler _liberarAteData;
    private readonly IniciarTrialAdminCommandHandler _iniciarTrial;
    private readonly SuspenderAssinaturaAdminCommandHandler _suspender;
    private readonly ReativarAssinaturaAdminCommandHandler _reativar;

    public AdminAssinaturasController(
        ListarHistoricoAssinaturasAdminQueryHandler listarHistorico,
        TrocarPlanoAdminCommandHandler trocarPlano,
        ConcederGratuidadeAdminCommandHandler concederGratuidade,
        EncerrarAssinaturaAdminCommandHandler encerrar,
        LiberarVitalicioAdminCommandHandler liberarVitalicio,
        LiberarAteDataAdminCommandHandler liberarAteData,
        IniciarTrialAdminCommandHandler iniciarTrial,
        SuspenderAssinaturaAdminCommandHandler suspender,
        ReativarAssinaturaAdminCommandHandler reativar)
    {
        _listarHistorico = listarHistorico;
        _trocarPlano = trocarPlano;
        _concederGratuidade = concederGratuidade;
        _encerrar = encerrar;
        _liberarVitalicio = liberarVitalicio;
        _liberarAteData = liberarAteData;
        _iniciarTrial = iniciarTrial;
        _suspender = suspender;
        _reativar = reativar;
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

    /// <summary>Libera o estabelecimento de forma vitalicia com o plano informado (CA24).</summary>
    [HttpPost("api/admin/estabelecimentos/{eid:long}/assinaturas/liberar-vitalicio")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LiberarVitalicio(
        long eid,
        [FromBody] LiberarVitalicioRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _liberarVitalicio.Handle(new LiberarVitalicioAdminCommand(
            eid, request.PlanoId, request.Motivo, adminId), ct);
        return NoContent();
    }

    /// <summary>Libera o estabelecimento ate uma data especifica (CA25).</summary>
    [HttpPost("api/admin/estabelecimentos/{eid:long}/assinaturas/liberar-ate-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LiberarAteData(
        long eid,
        [FromBody] LiberarAteDataRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _liberarAteData.Handle(new LiberarAteDataAdminCommand(
            eid, request.PlanoId, request.DataExpiracao, request.Motivo, adminId), ct);
        return NoContent();
    }

    /// <summary>Inicia trial de N dias para o estabelecimento (CA26).</summary>
    [HttpPost("api/admin/estabelecimentos/{eid:long}/assinaturas/iniciar-trial")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> IniciarTrial(
        long eid,
        [FromBody] IniciarTrialRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _iniciarTrial.Handle(new IniciarTrialAdminCommand(
            eid, request.Dias, request.PlanoId, request.Motivo, adminId), ct);
        return NoContent();
    }

    /// <summary>Suspende a vigencia atual do estabelecimento (CA27).</summary>
    [HttpPost("api/admin/estabelecimentos/{eid:long}/assinaturas/suspender")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Suspender(
        long eid,
        [FromBody] AssinaturaMotivoRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _suspender.Handle(new SuspenderAssinaturaAdminCommand(eid, request.Motivo, adminId), ct);
        return NoContent();
    }

    /// <summary>Remove a suspensao da vigencia atual (CA27).</summary>
    [HttpPost("api/admin/estabelecimentos/{eid:long}/assinaturas/reativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reativar(
        long eid,
        [FromBody] AssinaturaMotivoRequest request,
        CancellationToken ct = default)
    {
        var adminId = ObterAdminId();
        await _reativar.Handle(new ReativarAssinaturaAdminCommand(eid, request.Motivo, adminId), ct);
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
public record LiberarVitalicioRequest(Guid PlanoId, string Motivo);
public record LiberarAteDataRequest(Guid PlanoId, DateTimeOffset DataExpiracao, string Motivo);
public record IniciarTrialRequest(int Dias, Guid PlanoId, string Motivo);
public record AssinaturaMotivoRequest(string Motivo);
