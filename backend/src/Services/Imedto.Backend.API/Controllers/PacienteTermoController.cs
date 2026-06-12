using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.Contracts.Termos.Queries;
using Imedto.Backend.Infrastructure.Termos;
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
    private readonly ITermoPdfGeradoService _pdfGerado;

    public PacienteTermoController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant,
        ITermoPdfGeradoService pdfGerado)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
        _pdfGerado = pdfGerado;
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
    /// Emite um termo de consentimento (documento físico). Body: <c>{ modeloId, profissionalUsuarioId? }</c>.
    /// O termo é criado com status Pendente. O documento assinado é enviado separadamente via
    /// <c>POST /api/termos/{id}/pdf</c> (aceita JPG, PNG ou PDF).
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
            EvolucaoId = request.EvolucaoId,
        };
        await _commandBus.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { pacienteId, id = cmd.TermoEmitidoId },
            new { termoEmitidoId = cmd.TermoEmitidoId });
    }

    /// <summary>
    /// Upload do documento assinado (multipart). Aceita PDF (application/pdf), JPG (image/jpeg)
    /// ou PNG (image/png). Máximo 10 MB total. Imagens são convertidas para PDF multi-página
    /// (frente + verso = 2 páginas) no backend via QuestPDF.
    /// </summary>
    [HttpPost("api/termos/{id:long}/pdf")]
    [RequiresAcao("termos", "emitir")]
    [Idempotent]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AnexarPdf(long id, [FromForm] IFormFileCollection arquivos)
    {
        if (arquivos is null || arquivos.Count == 0)
            return UnprocessableEntity(new { tipo = "ErroDeNegocio", mensagem = "Arquivo vazio." });

        var cmd = new AnexarPdfTermoCommand
        {
            TermoEmitidoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
        };

        // Converte a coleção de IFormFile para lista de streams + mimes
        var partes = new List<(Stream stream, string mime, long tamanho)>();
        foreach (var arq in arquivos)
        {
            partes.Add((arq.OpenReadStream(), arq.ContentType ?? "", arq.Length));
        }
        cmd.Partes = partes;
        cmd.TamanhoTotalBytes = arquivos.Sum(a => a.Length);

        try
        {
            await _commandBus.Send(cmd);
        }
        finally
        {
            foreach (var (s, _, _) in partes)
                await s.DisposeAsync();
        }

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
    /// Gera e devolve o PDF probatório do termo a partir do snapshot HTML.
    /// Inclui cabeçalho institucional, bloco do paciente, snapshot da versão aceita,
    /// bloco de evidência do aceite (data/hora, identificação, IP/UA, hash, últimos 6 chars do token)
    /// e marca d'água por status.
    /// Multi-tenant: termo de outro estabelecimento devolve 422 genérico.
    /// Audit LGPD best-effort em <c>termo_audit_log</c> (ação: "termo-pdf-gerado").
    /// CA11: nome do arquivo <c>termo-{id}.pdf</c> — sem PII.
    /// </summary>
    [HttpGet("api/termos/{id:long}/pdf-gerado")]
    [RequiresAcao("termos", "emitir")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ObterPdfGerado(long id)
    {
        var bytes = await _pdfGerado.GerarAsync(id, _tenant.EstabelecimentoId, _tenant.UsuarioId);
        return File(bytes, "application/pdf", $"termo-{id}.pdf");
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

}

public record EmitirTermoRequest(
    long ModeloId,
    Guid? ProfissionalUsuarioId = null,
    long? EvolucaoId = null);
public record RevogarTermoRequest(string Motivo);
