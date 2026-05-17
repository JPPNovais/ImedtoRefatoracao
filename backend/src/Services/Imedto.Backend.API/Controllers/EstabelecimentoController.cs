using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Contracts.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Estabelecimentos.Queries.Results;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.API.Controllers;

/// <summary>Gerenciamento dos estabelecimentos do usuário autenticado.</summary>
[Authorize]
[ApiController]
[Route("api/estabelecimento")]
[Produces("application/json")]
[RequestSizeLimit(5 * 1024 * 1024)]
public class EstabelecimentoController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;

    public EstabelecimentoController(ICommandBus commandBus, IRequestBus requestBus)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
    }

    /// <summary>Lista todos os estabelecimentos que o usuário autenticado tem acesso.</summary>
    /// <response code="200">Lista (pode estar vazia).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EstabelecimentoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        var resultado = await _requestBus.Query<ListarMeusEstabelecimentosQuery, IEnumerable<EstabelecimentoDto>>(
            new ListarMeusEstabelecimentosQuery { UsuarioId = userId });

        return Ok(resultado);
    }

    /// <summary>
    /// Verifica se um CNPJ é válido (algoritmo padrão) e está disponível
    /// (não cadastrado em outro estabelecimento). Usado pelo onboarding inline.
    /// </summary>
    /// <response code="200">Resultado da verificação.</response>
    [HttpGet("cnpj-disponivel")]
    [ProducesResponseType(typeof(VerificarCnpjDisponivelResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<VerificarCnpjDisponivelResult>> VerificarCnpjDisponivel(
        [FromQuery] string cnpj)
    {
        var resultado = await _requestBus.Query<VerificarCnpjDisponivelQuery, VerificarCnpjDisponivelResult>(
            new VerificarCnpjDisponivelQuery { Cnpj = cnpj ?? "" });
        return Ok(resultado);
    }

    /// <summary>Cria um novo estabelecimento (o usuário autenticado vira dono).</summary>
    /// <response code="201">Criado.</response>
    /// <response code="422">Dados inválidos ou CNPJ duplicado.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarEstabelecimentoRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new CriarEstabelecimentoCommand
        {
            DonoUsuarioId = userId,
            NomeFantasia = request.NomeFantasia,
            RazaoSocial = request.RazaoSocial,
            Cnpj = request.Cnpj,
            Telefone = request.Telefone,
            Endereco = request.Endereco
        });

        return Created(string.Empty, null);
    }

    /// <summary>Atualiza dados do estabelecimento. Dono ou usuário com permissão de configuração.</summary>
    /// <response code="204">Atualizado.</response>
    /// <response code="404">Estabelecimento não encontrado.</response>
    /// <response code="422">Dados inválidos ou sem permissão.</response>
    [HttpPut("{id:long}")]
    [RequiresPermissaoExtra(PermissoesExtras.ConfigEstabelecimento, "id")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(long id, [FromBody] AtualizarEstabelecimentoRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new AtualizarEstabelecimentoCommand
        {
            EstabelecimentoId = id,
            UsuarioSolicitanteId = userId,
            NomeFantasia = request.NomeFantasia,
            RazaoSocial = request.RazaoSocial,
            Cnpj = request.Cnpj,
            Telefone = request.Telefone,
            Endereco = request.Endereco
        });

        return NoContent();
    }

    /// <summary>Atualiza configuração de funcionamento (horários, dias, bloqueios). Dono ou usuário com permissão de configuração.</summary>
    /// <response code="204">Atualizado.</response>
    /// <response code="404">Estabelecimento não encontrado.</response>
    /// <response code="422">Dados inválidos ou usuário não é dono.</response>
    [HttpPut("{id:long}/funcionamento")]
    [RequiresPermissaoExtra(PermissoesExtras.ConfigEstabelecimento, "id")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AtualizarFuncionamento(long id, [FromBody] AtualizarFuncionamentoRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new AtualizarFuncionamentoCommand
        {
            EstabelecimentoId = id,
            UsuarioSolicitanteId = userId,
            HorarioInicio = request.HorarioInicio,
            HorarioFim = request.HorarioFim,
            DuracaoConsultaPadraoMinutos = request.DuracaoConsultaPadraoMinutos,
            IntervaloEntreConsultasMinutos = request.IntervaloEntreConsultasMinutos,
            DiasSemana = request.DiasSemana ?? Array.Empty<int>(),
            HorariosBloqueados = (request.HorariosBloqueados ?? Array.Empty<HorarioBloqueadoBody>())
                .Select(h => new HorarioBloqueadoInput(h.Id, h.Inicio, h.Fim, h.Descricao ?? string.Empty))
                .ToList(),
            DatasBloqueadas = (request.DatasBloqueadas ?? Array.Empty<DataBloqueadaBody>())
                .Select(d => new DataBloqueadaInput(d.Id, d.Data, d.Descricao ?? string.Empty))
                .ToList(),
        });

        return NoContent();
    }

    /// <summary>Faz upload da foto/logo do estabelecimento (multipart). Dono ou usuário com permissão de configuração.</summary>
    /// <remarks>Aceita JPG, PNG, WebP e GIF até 2 MB. Retorna a URL pública atualizada.</remarks>
    /// <response code="200">Foto atualizada — corpo: <c>{ fotoUrl }</c>.</response>
    /// <response code="422">Arquivo inválido ou sem permissão.</response>
    [HttpPut("{id:long}/foto")]
    [RequiresPermissaoExtra(PermissoesExtras.ConfigEstabelecimento, "id")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AlterarFoto(long id, [FromForm] IFormFile arquivo)
    {
        var validacao = ValidarFoto(arquivo);
        if (validacao is { } erro)
            return UnprocessableEntity(new { tipo = "ErroDeNegocio", mensagem = erro });

        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var ext = Path.GetExtension(arquivo.FileName);

        await using var stream = arquivo.OpenReadStream();
        await _commandBus.Send(new AlterarFotoEstabelecimentoCommand
        {
            EstabelecimentoId = id,
            UsuarioSolicitanteId = userId,
            MimeType = arquivo.ContentType,
            Extensao = ext,
            Conteudo = stream
        });

        // Lista para encontrar a URL atualizada (não há query unitária por id no momento).
        var lista = await _requestBus.Query<ListarMeusEstabelecimentosQuery, IEnumerable<EstabelecimentoDto>>(
            new ListarMeusEstabelecimentosQuery { UsuarioId = userId });
        var atualizado = lista.FirstOrDefault(e => e.Id == id);

        return Ok(new { fotoUrl = atualizado?.FotoUrl });
    }

    /// <summary>Remove a foto/logo do estabelecimento (idempotente). Dono ou usuário com permissão de configuração.</summary>
    /// <response code="204">Foto removida (ou já estava ausente).</response>
    /// <response code="404">Estabelecimento não encontrado.</response>
    /// <response code="422">Sem permissão.</response>
    [HttpDelete("{id:long}/foto")]
    [RequiresPermissaoExtra(PermissoesExtras.ConfigEstabelecimento, "id")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoverFoto(long id)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new RemoverFotoEstabelecimentoCommand
        {
            EstabelecimentoId = id,
            UsuarioSolicitanteId = userId,
        });

        return NoContent();
    }

    private static string ValidarFoto(IFormFile arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
            return "Arquivo vazio.";
        if (arquivo.Length > 2 * 1024 * 1024)
            return "Foto deve ter no máximo 2 MB.";

        var mimesAceitos = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!mimesAceitos.Contains(arquivo.ContentType?.ToLowerInvariant()))
            return "Formato não suportado. Use JPG, PNG, WebP ou GIF.";

        return null;
    }
}

public record CriarEstabelecimentoRequest(
    string NomeFantasia,
    string RazaoSocial,
    string Cnpj,
    string Telefone,
    string Endereco);

public record AtualizarEstabelecimentoRequest(
    string NomeFantasia,
    string RazaoSocial,
    string Cnpj,
    string Telefone,
    string Endereco);

public record AtualizarFuncionamentoRequest(
    TimeOnly HorarioInicio,
    TimeOnly HorarioFim,
    int DuracaoConsultaPadraoMinutos,
    int IntervaloEntreConsultasMinutos,
    IReadOnlyList<int> DiasSemana,
    IReadOnlyList<HorarioBloqueadoBody> HorariosBloqueados,
    IReadOnlyList<DataBloqueadaBody> DatasBloqueadas);

public record HorarioBloqueadoBody(Guid? Id, TimeOnly Inicio, TimeOnly Fim, string Descricao);

public record DataBloqueadaBody(Guid? Id, DateOnly Data, string Descricao);
