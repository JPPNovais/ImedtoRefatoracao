using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.Contracts.Termos.Queries;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Termos emitidos para um paciente. Acesso restrito a quem tem
/// <c>termos.emitir</c> (recepção/médico) ou <c>termos.gerenciar_modelos</c>
/// (dono/admin) — para revogar.
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[ApiController]
[Produces("application/json")]
public class PacienteTermoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public PacienteTermoController(ICommandBus commandBus, IRequestBus requestBus, ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    [HttpGet("api/pacientes/{pacienteId:long}/termos")]
    [RequiresAcao("termos", "emitir")]
    [ProducesResponseType(typeof(IReadOnlyList<TermoEmitidoResumoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarDoPaciente(long pacienteId, [FromQuery] string status = null)
    {
        var dto = await _requestBus.Query<ListarTermosDoPacienteQuery, IReadOnlyList<TermoEmitidoResumoDto>>(
            new ListarTermosDoPacienteQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                Status = status,
            });
        return Ok(dto);
    }

    [HttpGet("api/pacientes/{pacienteId:long}/termos/{id:long}")]
    [RequiresAcao("termos", "emitir")]
    [ProducesResponseType(typeof(TermoEmitidoDetalheDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Obter(long pacienteId, long id)
    {
        var dto = await _requestBus.Query<ObterTermoEmitidoQuery, TermoEmitidoDetalheDto>(new ObterTermoEmitidoQuery
        {
            PacienteId = pacienteId,
            TermoEmitidoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
        });
        return Ok(dto);
    }

    /// <summary>
    /// Emite um termo. Body: <c>{ modeloId, assinaturaTipo: "pdf_anexado" | "aceite_link", canalEnvio?: "email" | "copia" }</c>.
    /// <para><c>canalEnvio</c> só é considerado quando <c>assinaturaTipo = aceite_link</c>:
    /// "email" (default) dispara o envio automático; "copia" suprime o e-mail e retorna o token
    /// para o emissor copiar o link.</para>
    /// Idempotente via header <c>Idempotency-Key</c>.
    /// </summary>
    [HttpPost("api/pacientes/{pacienteId:long}/termos")]
    [RequiresAcao("termos", "emitir")]
    [Idempotent]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Emitir(long pacienteId, [FromBody] EmitirTermoRequest request)
    {
        var cmd = new EmitirTermoCommand
        {
            PacienteId = pacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            EmissorUsuarioId = _tenant.UsuarioId,
            ProfissionalUsuarioId = request.ProfissionalUsuarioId,
            ModeloId = request.ModeloId,
            AssinaturaTipo = request.AssinaturaTipo,
            CanalEnvio = string.IsNullOrWhiteSpace(request.CanalEnvio) ? "email" : request.CanalEnvio,
        };
        await _commandBus.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { pacienteId, id = cmd.TermoEmitidoId },
            new
            {
                termoEmitidoId = cmd.TermoEmitidoId,
                tokenAceite = cmd.TokenAceiteGerado, // null se pdf_anexado
            });
    }

    /// <summary>Upload do PDF assinado (multipart). Máximo 10 MB.</summary>
    [HttpPost("api/termos/{id:long}/pdf")]
    [RequiresAcao("termos", "emitir")]
    [Idempotent]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AnexarPdf(long id, [FromForm] IFormFile arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
            return UnprocessableEntity(new { tipo = "ErroDeNegocio", mensagem = "Arquivo vazio." });

        await using var stream = arquivo.OpenReadStream();
        var cmd = new AnexarPdfTermoCommand
        {
            TermoEmitidoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            NomeOriginal = arquivo.FileName,
            MimeType = arquivo.ContentType,
            TamanhoBytes = arquivo.Length,
            Conteudo = stream,
        };
        await _commandBus.Send(cmd);
        return StatusCode(StatusCodes.Status201Created, new
        {
            storagePath = cmd.StoragePath,
            pdfHash = cmd.PdfHash,
        });
    }

    /// <summary>Gera URL presigned (TTL 5min) para baixar o PDF anexado.</summary>
    [HttpGet("api/termos/{id:long}/pdf")]
    [RequiresAcao("termos", "emitir")]
    [ProducesResponseType(typeof(TermoPdfUrlDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterUrlPdf(long id)
    {
        var dto = await _requestBus.Query<ObterUrlPdfTermoQuery, TermoPdfUrlDto>(new ObterUrlPdfTermoQuery
        {
            TermoEmitidoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
        });
        return Ok(dto);
    }

    /// <summary>
    /// Stub Fase 3 — geração de PDF on-the-fly a partir do snapshot HTML. Não implementado
    /// nessa fase. Responde 501.
    /// </summary>
    [HttpGet("api/termos/{id:long}/pdf-gerado")]
    [RequiresAcao("termos", "emitir")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult ObterPdfGerado(long id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            tipo = "NaoImplementado",
            mensagem = "Geração de PDF a partir do snapshot ainda não disponível (planejado para a Fase 3).",
        });
    }

    [HttpPost("api/termos/{id:long}/revogar")]
    [RequiresAcao("termos", "gerenciar_modelos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Revogar(long id, [FromBody] RevogarTermoRequest request)
    {
        await _commandBus.Send(new RevogarTermoCommand
        {
            TermoEmitidoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            Motivo = request.Motivo,
        });
        return NoContent();
    }

    /// <summary>
    /// Reenvia o link público (Fase 4). Body: <c>{ canal?: "email" | "copia" }</c>.
    /// <list type="bullet">
    ///   <item>"email" (default): envia e-mail ao paciente. Cooldown de 5 min entre envios.</item>
    ///   <item>"copia": não envia e-mail — só devolve o token pro front exibir/copiar.</item>
    /// </list>
    /// </summary>
    [HttpPost("api/termos/{id:long}/reenviar-link")]
    [RequiresAcao("termos", "emitir")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReenviarLink(long id, [FromBody] ReenviarLinkRequest request)
    {
        var cmd = new ReenviarLinkTermoCommand
        {
            TermoEmitidoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            Canal = string.IsNullOrWhiteSpace(request?.Canal) ? "email" : request.Canal,
        };
        await _commandBus.Send(cmd);
        return Ok(new
        {
            tokenAceite = cmd.TokenAceite,
            canal = cmd.Canal,
        });
    }
}

public record EmitirTermoRequest(
    long ModeloId,
    string AssinaturaTipo,
    string CanalEnvio = "email",
    Guid? ProfissionalUsuarioId = null);
public record RevogarTermoRequest(string Motivo);
public record ReenviarLinkRequest(string Canal = "email");
