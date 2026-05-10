using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Queries.Results;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Endpoints de orçamento (aggregate único). Não há mais distinção entre orçamento
/// "simples" e "completo" — todos os endpoints aceitam todas as collections opcionais.
/// </summary>
[ApiController]
[Route("api/orcamentos")]
[Authorize]
[RequiresEstabelecimento]
[RequiresAcao("orcamento")]
public class OrcamentoController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public OrcamentoController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrcamentoResumoDto>>> Listar(
        [FromQuery] long? pacienteId,
        [FromQuery] string? status)
    {
        var result = await _query.Query<ListarOrcamentosQuery, IEnumerable<OrcamentoResumoDto>>(
            new ListarOrcamentosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                PacienteId = pacienteId,
                Status = status
            });
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrcamentoDto>> Obter(long id)
    {
        var dto = await _query.Query<ObterOrcamentoQuery, OrcamentoDto>(
            new ObterOrcamentoQuery
            {
                OrcamentoId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId
            });
        return Ok(dto);
    }

    [HttpPost]
    [Idempotent]
    [FeatureGate(Features.OrcamentoCompleto)]
    public async Task<ActionResult> Criar([FromBody] CriarOrcamentoDto dto)
    {
        var cmd = new CriarOrcamentoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            PacienteId = dto.PacienteId,
            Validade = dto.Validade,
            Observacoes = dto.Observacoes,
            CriadoPorUsuarioId = _tenant.UsuarioId,
            ProcedimentoCirurgicoId = dto.ProcedimentoCirurgicoId,
            Itens = dto.Itens?.Select(i =>
                new ItemOrcamentoPayload(i.Descricao, i.Quantidade, i.ValorUnitario, i.DescontoPercent)).ToList() ?? new(),
            Equipe = dto.Equipe?.Select(e =>
                new OrcamentoEquipePayload(e.ProfissionalUsuarioId, e.Papel, e.Valor)).ToList() ?? new(),
            Implantes = dto.Implantes?.Select(i =>
                new OrcamentoImplantePayload(i.ItemInventarioId, i.Descricao, i.Quantidade, i.CustoUnitario)).ToList() ?? new(),
            FormasPagamento = dto.FormasPagamento?.Select(f =>
                new OrcamentoFormaPagamentoPayload(
                    f.FormaPagamentoId, f.Valor, f.Parcelas,
                    f.AcrescimoPercentual, f.EntradaPercentual, f.Observacao)).ToList() ?? new(),
            Cirurgias = dto.Cirurgias?.Select(c =>
                new OrcamentoCirurgiaPayload(
                    c.ProcedimentoCirurgicoId, c.Descricao, c.Quantidade, c.DuracaoMinutos, c.ValorTotal)).ToList() ?? new(),
            Internacao = dto.Internacao is null
                ? null
                : new OrcamentoInternacaoPayload(dto.Internacao.Tipo, dto.Internacao.Dias, dto.Internacao.ValorDiaria),
            Anestesia = dto.Anestesia is null
                ? null
                : new OrcamentoAnestesiaPayload(dto.Anestesia.Tipo, dto.Anestesia.Valor, dto.Anestesia.Observacao)
        };

        await _cmd.Send(cmd);
        return CreatedAtAction(nameof(Obter), new { id = cmd.OrcamentoIdCriado },
            new { orcamentoId = cmd.OrcamentoIdCriado });
    }

    [HttpPut("{id:long}")]
    [FeatureGate(Features.OrcamentoCompleto)]
    public async Task<ActionResult> Atualizar(long id, [FromBody] AtualizarOrcamentoDto dto)
    {
        await _cmd.Send(new AtualizarOrcamentoCommand
        {
            OrcamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Validade = dto.Validade,
            Observacoes = dto.Observacoes,
            ProcedimentoCirurgicoId = dto.ProcedimentoCirurgicoId,
            Itens = dto.Itens?.Select(i =>
                new ItemOrcamentoPayload(i.Descricao, i.Quantidade, i.ValorUnitario, i.DescontoPercent)).ToList() ?? new(),
            Equipe = dto.Equipe?.Select(e =>
                new OrcamentoEquipePayload(e.ProfissionalUsuarioId, e.Papel, e.Valor)).ToList() ?? new(),
            Implantes = dto.Implantes?.Select(i =>
                new OrcamentoImplantePayload(i.ItemInventarioId, i.Descricao, i.Quantidade, i.CustoUnitario)).ToList() ?? new(),
            FormasPagamento = dto.FormasPagamento?.Select(f =>
                new OrcamentoFormaPagamentoPayload(
                    f.FormaPagamentoId, f.Valor, f.Parcelas,
                    f.AcrescimoPercentual, f.EntradaPercentual, f.Observacao)).ToList() ?? new(),
            Cirurgias = dto.Cirurgias?.Select(c =>
                new OrcamentoCirurgiaPayload(
                    c.ProcedimentoCirurgicoId, c.Descricao, c.Quantidade, c.DuracaoMinutos, c.ValorTotal)).ToList() ?? new(),
            Internacao = dto.Internacao is null
                ? null
                : new OrcamentoInternacaoPayload(dto.Internacao.Tipo, dto.Internacao.Dias, dto.Internacao.ValorDiaria),
            Anestesia = dto.Anestesia is null
                ? null
                : new OrcamentoAnestesiaPayload(dto.Anestesia.Tipo, dto.Anestesia.Valor, dto.Anestesia.Observacao)
        });
        return NoContent();
    }

    [HttpPost("{id:long}/enviar")]
    public async Task<ActionResult> Enviar(long id)
    {
        await _cmd.Send(new EnviarOrcamentoCommand
        {
            OrcamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    [HttpPost("{id:long}/aprovar")]
    public async Task<ActionResult> Aprovar(long id)
    {
        await _cmd.Send(new AprovarOrcamentoCommand
        {
            OrcamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    [HttpPost("{id:long}/recusar")]
    public async Task<ActionResult> Recusar(long id)
    {
        await _cmd.Send(new RecusarOrcamentoCommand
        {
            OrcamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    [HttpPost("{id:long}/cancelar")]
    public async Task<ActionResult> Cancelar(long id)
    {
        await _cmd.Send(new CancelarOrcamentoCommand
        {
            OrcamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    /// <summary>
    /// Cria um <c>ProcedimentoCirurgico</c> a partir de um orçamento aprovado e
    /// vincula-o ao orçamento. Idempotente — segunda chamada falha com 422.
    /// </summary>
    [HttpPost("{id:long}/converter-em-cirurgia")]
    public async Task<ActionResult> ConverterEmCirurgia(long id, [FromBody] ConverterOrcamentoEmCirurgiaDto? dto)
    {
        var cmd = new ConverterOrcamentoEmCirurgiaCommand
        {
            OrcamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            DataAgendada = dto?.DataAgendada,
        };
        await _cmd.Send(cmd);
        return Ok(new { procedimentoCirurgicoId = cmd.ProcedimentoCirurgicoIdCriado });
    }

    /// <summary>
    /// Recebe o estado em construção do orçamento e devolve totais calculados sem
    /// persistir. Frontend chama com debounce de 250ms ao editar o form.
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<PreviewOrcamentoDto>> Preview([FromBody] PreviewOrcamentoInputDto dto)
    {
        var data = await _query.Query<PreviewOrcamentoQuery, PreviewOrcamentoDto>(
            new PreviewOrcamentoQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Itens = dto.Itens?.Select(i => new ItemOrcamentoPayload(
                    i.Descricao, i.Quantidade, i.ValorUnitario, i.DescontoPercent)).ToList() ?? new(),
                Equipe = dto.Equipe?.Select(e => new OrcamentoEquipePayload(
                    e.ProfissionalUsuarioId, e.Papel, e.Valor)).ToList() ?? new(),
                Implantes = dto.Implantes?.Select(i => new OrcamentoImplantePayload(
                    i.ItemInventarioId, i.Descricao, i.Quantidade, i.CustoUnitario)).ToList() ?? new(),
                FormasPagamento = dto.FormasPagamento?.Select(f => new OrcamentoFormaPagamentoPayload(
                    f.FormaPagamentoId, f.Valor, f.Parcelas,
                    f.AcrescimoPercentual, f.EntradaPercentual, f.Observacao)).ToList() ?? new(),
                Cirurgias = dto.Cirurgias?.Select(c => new OrcamentoCirurgiaPayload(
                    c.ProcedimentoCirurgicoId, c.Descricao, c.Quantidade, c.DuracaoMinutos, c.ValorTotal)).ToList() ?? new(),
                Internacao = dto.Internacao is null
                    ? null
                    : new OrcamentoInternacaoPayload(dto.Internacao.Tipo, dto.Internacao.Dias, dto.Internacao.ValorDiaria),
                Anestesia = dto.Anestesia is null
                    ? null
                    : new OrcamentoAnestesiaPayload(dto.Anestesia.Tipo, dto.Anestesia.Valor, dto.Anestesia.Observacao),
            });
        return Ok(data);
    }
}

public record ConverterOrcamentoEmCirurgiaDto(DateTime? DataAgendada);

/// <summary>DTO de entrada do preview — mesma estrutura do <c>CriarOrcamentoDto</c> mas sem campos de cabeçalho (paciente, validade) que não influem no cálculo.</summary>
public record PreviewOrcamentoInputDto(
    List<ItemOrcamentoInputDto>? Itens,
    List<OrcamentoEquipeInputDto>? Equipe,
    List<OrcamentoImplanteInputDto>? Implantes,
    List<OrcamentoFormaPagamentoInputDto>? FormasPagamento,
    List<OrcamentoCirurgiaInputDto>? Cirurgias,
    OrcamentoInternacaoInputDto? Internacao,
    OrcamentoAnestesiaInputDto? Anestesia);

// DTOs de entrada
public record ItemOrcamentoInputDto(
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal DescontoPercent);

public record OrcamentoEquipeInputDto(Guid ProfissionalUsuarioId, string Papel, decimal Valor);

public record OrcamentoImplanteInputDto(
    long? ItemInventarioId,
    string Descricao,
    decimal Quantidade,
    decimal CustoUnitario);

public record OrcamentoFormaPagamentoInputDto(
    long FormaPagamentoId,
    decimal Valor,
    int Parcelas,
    decimal AcrescimoPercentual,
    decimal EntradaPercentual,
    string? Observacao);

public record OrcamentoCirurgiaInputDto(
    long? ProcedimentoCirurgicoId,
    string? Descricao,
    int Quantidade,
    int? DuracaoMinutos,
    decimal ValorTotal);

public record OrcamentoInternacaoInputDto(string Tipo, int Dias, decimal ValorDiaria);

public record OrcamentoAnestesiaInputDto(string Tipo, decimal Valor, string? Observacao);

public record CriarOrcamentoDto(
    long PacienteId,
    DateOnly Validade,
    string? Observacoes,
    long? ProcedimentoCirurgicoId,
    List<ItemOrcamentoInputDto>? Itens,
    List<OrcamentoEquipeInputDto>? Equipe,
    List<OrcamentoImplanteInputDto>? Implantes,
    List<OrcamentoFormaPagamentoInputDto>? FormasPagamento,
    List<OrcamentoCirurgiaInputDto>? Cirurgias,
    OrcamentoInternacaoInputDto? Internacao,
    OrcamentoAnestesiaInputDto? Anestesia);

public record AtualizarOrcamentoDto(
    DateOnly Validade,
    string? Observacoes,
    long? ProcedimentoCirurgicoId,
    List<ItemOrcamentoInputDto>? Itens,
    List<OrcamentoEquipeInputDto>? Equipe,
    List<OrcamentoImplanteInputDto>? Implantes,
    List<OrcamentoFormaPagamentoInputDto>? FormasPagamento,
    List<OrcamentoCirurgiaInputDto>? Cirurgias,
    OrcamentoInternacaoInputDto? Internacao,
    OrcamentoAnestesiaInputDto? Anestesia);
