using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    /// <summary>Lista os anexos do prontuário (opcionalmente filtrados pela evolução).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AnexoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(long pacienteId, [FromQuery] long? evolucaoId = null)
    {
        var lista = await _requestBus.Query<ListarAnexosDoProntuarioQuery, IEnumerable<AnexoDto>>(
            new ListarAnexosDoProntuarioQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                EvolucaoId = evolucaoId
            });
        return Ok(lista);
    }

    /// <summary>Upload de um anexo (multipart). Retorna o Id do anexo criado.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Upload(
        long pacienteId,
        [FromForm] IFormFile arquivo,
        [FromForm] long? evolucaoId = null)
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
            Conteudo = stream
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
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId
            });
        return Ok(dto);
    }
}
