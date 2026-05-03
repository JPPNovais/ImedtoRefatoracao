using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Infrastructure.Receitas;
using Imedto.Backend.Contracts.Receitas.Queries;
using Imedto.Backend.Contracts.Receitas.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Receitas/prescrições. Acesso restrito a Profissional/Dono — Recepcionista
/// não emite nem visualiza receita (medicação é PII clínico sensível, LGPD).
///
/// Roteamento:
/// - <c>POST /api/receitas</c> — emitir direto (atalho — sem rascunho).
/// - <c>POST /api/receitas/rascunho</c> — iniciar rascunho.
/// - <c>PUT  /api/receitas/{id}/rascunho</c> — autosave de rascunho.
/// - <c>POST /api/receitas/{id}/finalizar</c> — fechar rascunho como Emitida.
/// - <c>POST /api/receitas/{id}/cancelar</c> — cancelar (estado clínico).
/// - <c>POST /api/receitas/{id}/duplicar</c> — clonar uma existente.
/// - <c>GET  /api/receitas/{id}</c> — detalhe (com itens).
/// - <c>GET  /api/pacientes/{pacienteId}/receitas</c> — lista paginada.
/// - <c>GET  /api/receitas/{id}/pdf</c> — download PDF (501 enquanto Wave 4 não chega).
/// - <c>GET/PUT /api/receitas/configuracao</c> — config do estabelecimento (só dono no PUT).
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
[FeatureGate(Features.Receitas)]
[ApiController]
[Produces("application/json")]
public class ReceitaController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;
    private readonly IReceitaPdfService _pdfService;

    public ReceitaController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant,
        IReceitaPdfService pdfService)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
        _pdfService = pdfService;
    }

    /// <summary>Emite uma nova receita para um paciente (atalho — sem rascunho).</summary>
    [HttpPost("api/receitas")]
    [Idempotent]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Emitir([FromBody] EmitirReceitaRequest request)
    {
        var cmd = new EmitirReceitaCommand
        {
            PacienteId = request.PacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ProfissionalUsuarioId = _tenant.UsuarioId,
            Tipo = request.Tipo,
            TipoNotificacao = request.TipoNotificacao,
            ValidadeAte = request.ValidadeAte,
            Observacoes = request.Observacoes,
            Itens = MapearItens(request.Itens)
        };

        await _commandBus.Send(cmd);

        return CreatedAtAction(nameof(Obter), new { id = cmd.ReceitaIdCriada },
            new { receitaId = cmd.ReceitaIdCriada });
    }

    /// <summary>
    /// Inicia uma receita em rascunho. Itens podem vir vazios; autosave preenche depois.
    /// LGPD: rascunho não consta como Escrita até virar <c>Emitida</c>.
    /// </summary>
    [HttpPost("api/receitas/rascunho")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> IniciarRascunho([FromBody] IniciarRascunhoReceitaRequest request)
    {
        var cmd = new IniciarRascunhoReceitaCommand
        {
            PacienteId = request.PacienteId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ProfissionalUsuarioId = _tenant.UsuarioId,
            Tipo = request.Tipo,
            TipoNotificacao = request.TipoNotificacao,
            ValidadeAte = request.ValidadeAte,
            Observacoes = request.Observacoes,
            Itens = MapearItens(request.Itens)
        };

        await _commandBus.Send(cmd);

        return CreatedAtAction(nameof(Obter), new { id = cmd.ReceitaIdCriada },
            new { receitaId = cmd.ReceitaIdCriada });
    }

    /// <summary>Atualiza um rascunho (autosave). Substitui observações e itens.</summary>
    [HttpPut("api/receitas/{id:long}/rascunho")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AtualizarRascunho(long id, [FromBody] AtualizarRascunhoReceitaRequest request)
    {
        await _commandBus.Send(new AtualizarRascunhoReceitaCommand
        {
            ReceitaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            Observacoes = request.Observacoes,
            Itens = MapearItens(request.Itens)
        });
        return NoContent();
    }

    /// <summary>Finaliza um rascunho — vira <c>Emitida</c>.</summary>
    [HttpPost("api/receitas/{id:long}/finalizar")]
    [Idempotent]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Finalizar(long id)
    {
        await _commandBus.Send(new FinalizarReceitaCommand
        {
            ReceitaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId
        });
        return NoContent();
    }

    private static List<ItemReceitaPayload> MapearItens(IEnumerable<ItemReceitaRequest>? itens) =>
        itens?.Select(i => new ItemReceitaPayload
        {
            Medicamento = i.Medicamento,
            Posologia = i.Posologia,
            Quantidade = i.Quantidade,
            Via = i.Via,
            Observacao = i.Observacao,
            Concentracao = i.Concentracao,
            FormaFarmaceutica = i.FormaFarmaceutica,
            Duracao = i.Duracao
        }).ToList() ?? new List<ItemReceitaPayload>();

    /// <summary>Cancela uma receita emitida. Exige motivo.</summary>
    [HttpPost("api/receitas/{id:long}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancelar(long id, [FromBody] CancelarReceitaRequest request)
    {
        await _commandBus.Send(new CancelarReceitaCommand
        {
            ReceitaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            Motivo = request.Motivo
        });
        return NoContent();
    }

    /// <summary>Duplica receita existente — útil para repetir prescrição em nova consulta.</summary>
    [HttpPost("api/receitas/{id:long}/duplicar")]
    [Idempotent]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Duplicar(long id)
    {
        var cmd = new DuplicarReceitaCommand
        {
            ReceitaIdOrigem = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ProfissionalUsuarioId = _tenant.UsuarioId
        };
        await _commandBus.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { id = cmd.ReceitaIdCriada },
            new { receitaId = cmd.ReceitaIdCriada });
    }

    /// <summary>Detalhe da receita (com itens).</summary>
    [HttpGet("api/receitas/{id:long}")]
    [ProducesResponseType(typeof(ReceitaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long id)
    {
        var dto = await _requestBus.Query<ObterReceitaQuery, ReceitaDto>(new ObterReceitaQuery
        {
            ReceitaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId
        });
        return Ok(dto);
    }

    /// <summary>Listagem paginada das receitas do paciente.</summary>
    [HttpGet("api/pacientes/{pacienteId:long}/receitas")]
    [ProducesResponseType(typeof(PaginaReceitasDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarDoPaciente(
        long pacienteId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var dto = await _requestBus.Query<ListarReceitasDoPacienteQuery, PaginaReceitasDto>(
            new ListarReceitasDoPacienteQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });
        return Ok(dto);
    }

    /// <summary>Download do PDF da receita.</summary>
    [HttpGet("api/receitas/{id:long}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BaixarPdf(long id)
    {
        var bytes = await _pdfService.GerarAsync(id, _tenant.EstabelecimentoId);
        return File(bytes, "application/pdf", $"receita-{id}.pdf");
    }

    /// <summary>Configuração de receita do estabelecimento (cabeçalho/rodapé/emissor padrão).</summary>
    [HttpGet("api/receitas/configuracao")]
    [ProducesResponseType(typeof(ConfiguracaoReceitaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterConfiguracao()
    {
        var dto = await _requestBus.Query<ObterConfiguracaoReceitaQuery, ConfiguracaoReceitaDto>(
            new ObterConfiguracaoReceitaQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId
            });
        return Ok(dto);
    }

    /// <summary>Atualiza a configuração de receita. Restrito ao Dono do estabelecimento.</summary>
    [HttpPut("api/receitas/configuracao")]
    [RequiresPapel(TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AtualizarConfiguracao([FromBody] AtualizarConfiguracaoReceitaRequest request)
    {
        await _commandBus.Send(new AtualizarConfiguracaoReceitaCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            CabecalhoHtml = request.CabecalhoHtml,
            RodapeHtml = request.RodapeHtml,
            ModeloPadraoId = request.ModeloPadraoId,
            EmissorPadrao = request.EmissorPadrao
        });
        return NoContent();
    }

    // ListarFavoritos removido (decisao Fase 1): endpoint GET sem consumidor no front.
    // A escrita (registrar uso de medicamento favorito) continua via EmitirReceitaCommandHandler
    // -> IMedicamentoFavoritoRepository.RegistrarUso. Recriar o GET quando UI for implementada.
}

public record EmitirReceitaRequest(
    long PacienteId,
    string Tipo,
    DateTime? ValidadeAte,
    string? Observacoes,
    IEnumerable<ItemReceitaRequest>? Itens,
    string? TipoNotificacao = null);

public record IniciarRascunhoReceitaRequest(
    long PacienteId,
    string Tipo,
    DateTime? ValidadeAte,
    string? Observacoes,
    IEnumerable<ItemReceitaRequest>? Itens,
    string? TipoNotificacao = null);

public record AtualizarRascunhoReceitaRequest(
    string? Observacoes,
    IEnumerable<ItemReceitaRequest>? Itens);

public record ItemReceitaRequest(
    string Medicamento,
    string Posologia,
    string? Quantidade,
    string? Via,
    string? Observacao,
    string? Concentracao = null,
    string? FormaFarmaceutica = null,
    string? Duracao = null);

public record CancelarReceitaRequest(string Motivo);

public record AtualizarConfiguracaoReceitaRequest(
    string? CabecalhoHtml,
    string? RodapeHtml,
    long? ModeloPadraoId,
    string? EmissorPadrao);
