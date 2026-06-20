using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Contracts.Prontuarios.Queries;
using Imedto.Backend.Contracts.Prontuarios.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Anexos do prontuário — upload via multipart (proxy pelo backend) e download via URL
/// assinada temporária (300s por padrão). Limite de upload: 50 MB (validação real fica no
/// command handler + storage; o RequestSizeLimit aqui é o teto do pipeline ASP.NET).
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]  // anexos clínicos são mutação; bloqueia uploads quando inativa
[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
[ApiController]
[Route("api/paciente/{pacienteId:long}/prontuario/anexos")]
[Produces("application/json")]
[RequestSizeLimit(60 * 1024 * 1024)]
public class ProntuarioAnexoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public ProntuarioAnexoController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>
    /// Lista os anexos do prontuário paginados (opcionalmente filtrados pela evolução).
    /// Retrocompatível: sem <c>pagina</c>/<c>tamanho</c> retorna a 1ª página com 50 itens
    /// (comportamento equivalente ao anterior para quem não informa os parâmetros).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaAnexosDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        long pacienteId,
        [FromQuery] long? evolucaoId = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 50)
    {
        var dto = await _requestBus.Query<ListarAnexosDoProntuarioQuery, PaginaAnexosDto>(
            new ListarAnexosDoProntuarioQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                EvolucaoId = evolucaoId,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });
        return Ok(dto);
    }

    /// <summary>
    /// Batch de URLs assinadas: gera URLs de download temporárias para múltiplos anexos em
    /// uma única chamada. Elimina o N+1 de chamadas individuais ao baixar uma galeria de fotos.
    /// Defense-in-depth: anexoIds de outro paciente/tenant são silenciosamente ignorados.
    /// TTL igual ao endpoint individual (StorageOptions.TtlSignedUrlMinutos).
    /// </summary>
    [HttpPost("urls")]
    [ProducesResponseType(typeof(IEnumerable<AnexoUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ObterUrls(
        long pacienteId,
        [FromBody] AnexoIdsRequest request)
    {
        if (request?.AnexoIds is null || request.AnexoIds.Count == 0)
            return Ok(Array.Empty<AnexoUrlDto>());

        var dto = await _requestBus.Query<ObterUrlsAnexosQuery, IEnumerable<AnexoUrlDto>>(
            new ObterUrlsAnexosQuery
            {
                AnexoIds = request.AnexoIds,
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId
            });
        return Ok(dto);
    }

    /// <summary>Upload de um anexo (multipart). Retorna o Id do anexo criado.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Upload(
        long pacienteId,
        [FromForm] IFormFile arquivo,
        [FromForm] long? evolucaoId = null,
        [FromForm] string? regiaoAnatomica = null,
        [FromForm] string? marcador = null)
    {
        if (arquivo is null || arquivo.Length == 0)
            return UnprocessableEntity(new { tipo = "ErroDeNegocio", mensagem = "Arquivo vazio." });

        await using var stream = arquivo.OpenReadStream();
        var command = new AdicionarAnexoCommand
        {
            PacienteId = pacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            EvolucaoId = evolucaoId,
            AutorUsuarioId = _tenant.UsuarioId,
            NomeOriginal = arquivo.FileName,
            MimeType = arquivo.ContentType,
            TamanhoBytes = arquivo.Length,
            Conteudo = stream,
            RegiaoAnatomica = regiaoAnatomica,
            Marcador = marcador
        };

        await _commandBus.Send(command);

        return StatusCode(StatusCodes.Status201Created, new
        {
            anexoId = command.AnexoIdCriado,
            storagePath = command.StoragePath
        });
    }

    /// <summary>Gera URL assinada para download do anexo (expira em 5min).</summary>
    [HttpGet("{anexoId:long}/url")]
    [ProducesResponseType(typeof(AnexoUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterUrl(long pacienteId, long anexoId)
    {
        var dto = await _requestBus.Query<ObterUrlAnexoQuery, AnexoUrlDto>(
            new ObterUrlAnexoQuery
            {
                AnexoId = anexoId,
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId
            });
        return Ok(dto);
    }
}

/// <summary>Payload do endpoint de batch de URLs assinadas.</summary>
public record AnexoIdsRequest(IReadOnlyList<long> AnexoIds);
