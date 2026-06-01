using Imedto.Backend.Contracts.AssinaturaDigital.Commands;
using Imedto.Backend.Contracts.AssinaturaDigital.Queries;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Gerenciamento do certificado digital do médico.
/// Vínculo é por conta de usuário (não por estabelecimento) — D4 do briefing.
///
/// - <c>POST   /api/medico/certificado/vincular</c> — vincula certificado BirdID.
/// - <c>DELETE /api/medico/certificado</c>          — remove vínculo.
/// - <c>GET    /api/medico/certificado</c>           — retorna metadados (nunca o token).
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
[ApiController]
[Produces("application/json")]
public class MedicoCertificadoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public MedicoCertificadoController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>
    /// Vincula (ou atualiza) o certificado em nuvem ICP-Brasil do médico autenticado.
    /// CA-01: persiste refresh_token cifrado; provedor informado no payload.
    /// </summary>
    [HttpPost("api/medico/certificado/vincular")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Vincular([FromBody] VincularCertificadoRequest request)
    {
        await _commandBus.Send(new VincularCertificadoCommand
        {
            MedicoId = _tenant.UsuarioId,
            Provedor = request.Provedor,
            RefreshToken = request.RefreshToken,
            ExpiraEm = request.ExpiraEm,
        });
        return NoContent();
    }

    /// <summary>
    /// Remove o vínculo do certificado. CA-23: receitas pendentes continuam aguardando webhook.
    /// </summary>
    [HttpDelete("api/medico/certificado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover()
    {
        await _commandBus.Send(new RemoverCertificadoCommand
        {
            MedicoId = _tenant.UsuarioId,
        });
        return NoContent();
    }

    /// <summary>
    /// Retorna metadados do certificado vinculado. CA-14: nunca expõe o refresh_token.
    /// Retorna 200 com null se não houver certificado vinculado.
    /// </summary>
    [HttpGet("api/medico/certificado")]
    [ProducesResponseType(typeof(CertificadoVinculadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obter()
    {
        var dto = await _requestBus.Query<ObterCertificadoVinculadoQuery, CertificadoVinculadoDto?>(
            new ObterCertificadoVinculadoQuery
            {
                MedicoId = _tenant.UsuarioId,
            });
        return Ok(dto);
    }
}

public record VincularCertificadoRequest(
    string Provedor,
    string RefreshToken,
    DateTime? ExpiraEm = null);
