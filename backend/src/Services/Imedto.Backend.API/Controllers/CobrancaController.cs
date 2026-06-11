using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Cobrancas.Commands;
using Imedto.Backend.Contracts.Cobrancas.Queries;
using Imedto.Backend.Contracts.Cobrancas.Queries.Results;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/cobrancas")]
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]
public class CobrancaController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;
    private readonly IModeloPermissaoRepository _permissaoRepo;

    public CobrancaController(
        ICommandBus cmd,
        IRequestBus query,
        ICurrentTenantAccessor tenant,
        IModeloPermissaoRepository permissaoRepo)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
        _permissaoRepo = permissaoRepo;
    }

    /// <summary>Retorna detalhes de cobrança + histórico para o PaymentModal (CA4/CA12).</summary>
    [HttpGet("por-agendamento/{agendamentoId:long}")]
    [RequiresAcao("financeiro_paciente", "ver")]
    public async Task<ActionResult<CobrancaDetalheDto>> ObterPorAgendamento(long agendamentoId)
    {
        var result = await _query.Query<ObterCobrancaDaAgendaQuery, CobrancaDetalheDto?>(
            new ObterCobrancaDaAgendaQuery
            {
                AgendamentoId = agendamentoId,
                EstabelecimentoId = _tenant.EstabelecimentoId
            });

        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>Registra um ou mais pagamentos (CA4/CA5/CA7/CA8/CA21 — INV-3 atômica).</summary>
    [HttpPost("{id:long}/pagamentos")]

    [RequiresAcao("financeiro_paciente", "registrar")]
    public async Task<ActionResult> RegistrarPagamentos(long id, [FromBody] RegistrarPagamentosDto dto)
    {
        // RBAC de desconto (INV-8): dono sempre pode; demais verificam permissão granular.
        bool podeDesconto;
        if (_tenant.EhDono)
        {
            podeDesconto = true;
        }
        else
        {
            var temOrcaprovar = await _permissaoRepo.UsuarioTemAcao(_tenant.UsuarioId, _tenant.EstabelecimentoId, "orcamento", "aprovar");
            var temFinanLancar = await _permissaoRepo.UsuarioTemAcao(_tenant.UsuarioId, _tenant.EstabelecimentoId, "financeiro", "lancar");
            var temFinanPacReg = await _permissaoRepo.UsuarioTemAcao(_tenant.UsuarioId, _tenant.EstabelecimentoId, "financeiro_paciente", "registrar");
            podeDesconto = temOrcaprovar || temFinanLancar || temFinanPacReg;
        }

        await _cmd.Send(new RegistrarPagamentosCommand
        {
            CobrancaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioId = _tenant.UsuarioId,
            Desconto = dto.Desconto,
            PodeAplicarDesconto = podeDesconto,
            DataPagamento = dto.DataPagamento,
            Formas = dto.Formas.Select(f => new FormaPagamentoItem
            {
                FormaPagamentoId = f.FormaPagamentoId,
                Valor = f.Valor,
                Parcelas = f.Parcelas,
                Juros = f.Juros,
            }).ToList()
        });
        return NoContent();
    }

    // ── Aba Financeiro do paciente (F2) ───────────────────────────────────────

    /// <summary>
    /// Retorna KPIs + cobranças/pagamentos/estornos do paciente (aba Financeiro — CA23/CA36).
    /// Audit de acesso LGPD: registrado no handler via IPacienteAcessoLogService (R10).
    /// </summary>
    [HttpGet("paciente/{pacienteId:long}/financeiro-aba")]
    [RequiresAcao("financeiro_paciente", "ver")]
    public async Task<ActionResult<FinanceiroAbaDto>> ObterFinanceiroAba(long pacienteId)
    {
        var result = await _query.Query<ObterFinanceiroAbaQuery, FinanceiroAbaDto>(
            new ObterFinanceiroAbaQuery
            {
                PacienteId = pacienteId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                UsuarioId = _tenant.UsuarioId,
            });
        return Ok(result);
    }

    /// <summary>
    /// Estorna um pagamento de uma cobrança (INV-7 — CA29/CA30/CA31/CA32).
    /// Atômico: EstornoPagamento + Lancamento negativo na mesma transação.
    /// </summary>
    [HttpPost("{id:long}/pagamentos/{pagamentoId:long}/estorno")]

    [RequiresAcao("financeiro_paciente", "registrar")]
    public async Task<ActionResult> EstornarPagamento(long id, long pagamentoId, [FromBody] EstornarPagamentoDto dto)
    {
        await _cmd.Send(new EstornarPagamentoCommand
        {
            CobrancaId = id,
            PagamentoId = pagamentoId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            UsuarioId = _tenant.UsuarioId,
            Motivo = dto.Motivo,
        });
        return NoContent();
    }

    // ── Recibo de pagamento (F8) ──────────────────────────────────────────

    /// <summary>
    /// Gera o PDF do recibo de um pagamento quitado (F8/CA118).
    /// - CA120: pagamento estornado → 422.
    /// - CA121: pagamento inexistente ou de outro tenant → 422 genérico "Não encontrado".
    /// - CA123: RBAC financeiro_paciente.ver.
    /// - CA124: multi-tenant falha-fechada (estabelecimento_id do tenant).
    /// - CA125: nome do arquivo sem PII.
    /// </summary>
    [HttpGet("pagamentos/{pagamentoId:long}/recibo")]
    [RequiresAcao("financeiro_paciente", "ver")]
    public async Task<ActionResult> EmitirRecibo(long pagamentoId)
    {
        var pdfBytes = await _query.Query<EmitirReciboPagamentoQuery, byte[]>(
            new EmitirReciboPagamentoQuery
            {
                PagamentoId = pagamentoId,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                UsuarioId = _tenant.UsuarioId,
            });

        // CA125: nome de arquivo sem PII do paciente
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"recibo-{pagamentoId}.pdf\"");
        return File(pdfBytes, "application/pdf");
    }

    // ── Valor sugerido (check-in) ──────────────────────────────────────────

    [HttpGet("valor-sugerido")]
    public async Task<ActionResult<ValorSugeridoCheckInDto>> ObterValorSugerido([FromQuery] Guid profissionalUsuarioId)
    {
        var result = await _query.Query<ObterValorSugeridoCheckInQuery, ValorSugeridoCheckInDto>(
            new ObterValorSugeridoCheckInQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                ProfissionalUsuarioId = profissionalUsuarioId
            });
        return Ok(result);
    }

    // ── Tabela de preços de consulta ────────────────────────────────────────

    [HttpGet("config/tabela-preco")]
    [RequiresAcao("configuracoes", "gerenciar")]
    public async Task<ActionResult<IEnumerable<TabelaPrecoConsultaDto>>> ListarTabelaPreco(
        [FromQuery] string? busca)
    {
        var result = await _query.Query<ListarTabelaPrecoConsultaQuery, IEnumerable<TabelaPrecoConsultaDto>>(
            new ListarTabelaPrecoConsultaQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                BuscaProfissional = busca
            });
        return Ok(result);
    }

    [HttpPost("config/tabela-preco")]

    [RequiresAcao("configuracoes", "gerenciar")]
    public async Task<ActionResult> SalvarTabelaPreco([FromBody] SalvarTabelaPrecoConsultaDto dto)
    {
        await _cmd.Send(new SalvarTabelaPrecoConsultaCommand
        {
            Id = dto.Id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ProfissionalId = dto.ProfissionalId,
            ValorSugerido = dto.ValorSugerido
        });
        return NoContent();
    }

    [HttpDelete("config/tabela-preco/{id:long}")]

    [RequiresAcao("configuracoes", "gerenciar")]
    public async Task<ActionResult> InativarTabelaPreco(long id)
    {
        await _cmd.Send(new InativarTabelaPrecoConsultaCommand
        {
            Id = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    // ── Config taxa por forma de pagamento ──────────────────────────────────

    [HttpGet("config/taxa-forma-pagamento")]
    [RequiresAcao("configuracoes", "gerenciar")]
    public async Task<ActionResult<IEnumerable<ConfigTaxaFormaPagamentoDto>>> ListarConfigTaxa()
    {
        var result = await _query.Query<ListarConfigTaxaFormaPagamentoQuery, IEnumerable<ConfigTaxaFormaPagamentoDto>>(
            new ListarConfigTaxaFormaPagamentoQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(result);
    }

    [HttpPost("config/taxa-forma-pagamento")]

    [RequiresAcao("configuracoes", "gerenciar")]
    public async Task<ActionResult> SalvarConfigTaxa([FromBody] SalvarConfigTaxaFormaPagamentoDto dto)
    {
        await _cmd.Send(new SalvarConfigTaxaFormaPagamentoCommand
        {
            Id = dto.Id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            FormaPagamentoId = dto.FormaPagamentoId,
            TaxaPercentual = dto.TaxaPercentual,
            Ativo = dto.Ativo
        });
        return NoContent();
    }
}

// ── DTOs de request ────────────────────────────────────────────────────────────

public record RegistrarPagamentosDto(
    decimal Desconto,
    DateOnly DataPagamento,
    List<FormaPagamentoItemDto> Formas);

public record FormaPagamentoItemDto(
    long FormaPagamentoId,
    decimal Valor,
    int Parcelas = 1,
    decimal Juros = 0m);

public record SalvarTabelaPrecoConsultaDto(
    long? Id,
    Guid? ProfissionalId,
    decimal ValorSugerido);

public record EstornarPagamentoDto(string Motivo);

public record SalvarConfigTaxaFormaPagamentoDto(
    long? Id,
    long FormaPagamentoId,
    decimal TaxaPercentual,
    bool Ativo = true);
