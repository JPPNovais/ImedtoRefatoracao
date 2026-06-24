using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Financeiro.Commands;
using Imedto.Backend.Contracts.Financeiro.Queries;
using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Time;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/financeiro")]
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]
[RequiresAcao("financeiro")]
public class FinanceiroController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public FinanceiroController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
    }

    [HttpGet("lancamentos")]
    public async Task<ActionResult<PaginaLancamentosDto>> Listar(
        [FromQuery] string? tipo,
        [FromQuery] string? status,
        [FromQuery] string? categoria,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10)
    {
        var result = await _query.Query<ListarLancamentosQuery, PaginaLancamentosDto>(
            new ListarLancamentosQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Tipo = tipo,
                Status = status,
                Categoria = categoria,
                DataInicio = dataInicio,
                DataFim = dataFim,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });
        return Ok(result);
    }

    [HttpGet("resumo")]
    public async Task<ActionResult<ResumoFinanceiroDto>> Resumo(
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim)
    {
        var result = await _query.Query<ObterResumoFinanceiroQuery, ResumoFinanceiroDto>(
            new ObterResumoFinanceiroQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim
            });
        return Ok(result);
    }

    [HttpPost("lancamentos")]
    [Idempotent]
    public async Task<ActionResult> Criar([FromBody] CriarLancamentoDto dto)
    {
        await _cmd.Send(new CriarLancamentoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Tipo = dto.Tipo,
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            DataVencimento = dto.DataVencimento,
            Categoria = dto.Categoria,
            OrcamentoId = dto.OrcamentoId,
            CriadoPorUsuarioId = _tenant.UsuarioId
        });
        return NoContent();
    }

    [HttpPut("lancamentos/{id:long}")]
    public async Task<ActionResult> Atualizar(long id, [FromBody] AtualizarLancamentoDto dto)
    {
        await _cmd.Send(new AtualizarLancamentoCommand
        {
            LancamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            DataVencimento = dto.DataVencimento,
            Categoria = dto.Categoria
        });
        return NoContent();
    }

    [HttpPost("lancamentos/{id:long}/pagar")]
    public async Task<ActionResult> Pagar(long id, [FromBody] PagarLancamentoDto? dto)
    {
        await _cmd.Send(new PagarLancamentoCommand
        {
            LancamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            DataPagamento = dto?.DataPagamento
        });
        return NoContent();
    }

    [HttpPost("lancamentos/{id:long}/cancelar")]
    public async Task<ActionResult> Cancelar(long id)
    {
        await _cmd.Send(new CancelarLancamentoCommand
        {
            LancamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    // -------------------- Categorias financeiras --------------------

    [HttpGet("categorias")]
    public async Task<ActionResult<IEnumerable<CategoriaFinanceiraDto>>> ListarCategorias(
        [FromQuery] string? tipo,
        [FromQuery] bool? ativas,
        [FromQuery] bool? padrao)
    {
        var result = await _query.Query<ListarCategoriasFinanceirasQuery, IEnumerable<CategoriaFinanceiraDto>>(
            new ListarCategoriasFinanceirasQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Tipo = tipo,
                Ativas = ativas,
                Padrao = padrao
            });
        return Ok(result);
    }

    [HttpPost("categorias")]
    public async Task<ActionResult> CriarCategoria([FromBody] CriarCategoriaFinanceiraDto dto)
    {
        await _cmd.Send(new CriarCategoriaFinanceiraCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            Tipo = dto.Tipo
        });
        return NoContent();
    }

    [HttpPut("categorias/{id:long}")]
    public async Task<ActionResult> AtualizarCategoria(long id, [FromBody] AtualizarCategoriaFinanceiraDto dto)
    {
        await _cmd.Send(new AtualizarCategoriaFinanceiraCommand
        {
            CategoriaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            Tipo = dto.Tipo
        });
        return NoContent();
    }

    [HttpPost("categorias/{id:long}/inativar")]
    public async Task<ActionResult> InativarCategoria(long id)
    {
        await _cmd.Send(new InativarCategoriaFinanceiraCommand
        {
            CategoriaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    [HttpPost("categorias/{id:long}/reativar")]
    public async Task<ActionResult> ReativarCategoria(long id)
    {
        await _cmd.Send(new ReativarCategoriaFinanceiraCommand
        {
            CategoriaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    // -------------------- Formas de pagamento --------------------

    [HttpGet("formas-pagamento")]
    public async Task<ActionResult<IEnumerable<FormaPagamentoDto>>> ListarFormasPagamento(
        [FromQuery] bool? ativas,
        [FromQuery] bool? padrao)
    {
        var result = await _query.Query<ListarFormasPagamentoQuery, IEnumerable<FormaPagamentoDto>>(
            new ListarFormasPagamentoQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Ativas = ativas,
                Padrao = padrao
            });
        return Ok(result);
    }

    [HttpPost("formas-pagamento")]
    public async Task<ActionResult> CriarFormaPagamento([FromBody] CriarFormaPagamentoDto dto)
    {
        await _cmd.Send(new CriarFormaPagamentoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome
        });
        return NoContent();
    }

    [HttpPut("formas-pagamento/{id:long}")]
    public async Task<ActionResult> AtualizarFormaPagamento(long id, [FromBody] AtualizarFormaPagamentoDto dto)
    {
        await _cmd.Send(new AtualizarFormaPagamentoCommand
        {
            FormaPagamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome
        });
        return NoContent();
    }

    [HttpPost("formas-pagamento/{id:long}/inativar")]
    public async Task<ActionResult> InativarFormaPagamento(long id)
    {
        await _cmd.Send(new InativarFormaPagamentoCommand
        {
            FormaPagamentoId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    // ─────────────────────────── F7 — KPIs / Extrato ───────────────────────────

    [HttpGet("kpis")]
    [RequiresAcao("financeiro.ver")]
    public async Task<ActionResult<KpisFinanceiroDto>> ObterKpis(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly dataFim)
    {
        var result = await _query.Query<ObterKpisFinanceiroQuery, KpisFinanceiroDto>(
            new ObterKpisFinanceiroQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim
            });
        return Ok(result);
    }

    [HttpGet("extrato/export")]
    [RequiresAcao("financeiro.ver")]
    public async Task<IActionResult> ExportarExtrato(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly dataFim,
        [FromQuery] string? tipo,
        [FromQuery] string? categoria,
        [FromQuery] string? formaPagamento,
        [FromQuery] string? origem)
    {
        var result = await _query.Query<ExportarExtratoQuery, ExportarExtratoResultDto>(
            new ExportarExtratoQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                UsuarioId = _tenant.UsuarioId,
                DataInicio = dataInicio,
                DataFim = dataFim,
                Tipo = tipo,
                Categoria = categoria,
                FormaPagamento = formaPagamento,
                Origem = origem
            });

        var csv = GerarCsv(result.Itens);
        var nomeArquivo = $"extrato-financeiro-{dataFim:yyyy-MM-dd}.csv";
        return File(csv, "text/csv; charset=utf-8", nomeArquivo);
    }

    // Gera CSV UTF-8 com BOM, separador ";", decimal vírgula (D2/padrão Excel pt-BR).
    // PII minimizado: apenas campos exibidos na tela (R9).
    private static byte[] GerarCsv(IReadOnlyList<LancamentoExtratoDto> itens)
    {
        var sb = new System.Text.StringBuilder();
        // Cabeçalho
        sb.AppendLine("Data;Descrição;Paciente;Categoria;Forma de Pagamento;Valor;Status");

        foreach (var l in itens)
        {
            var data = l.DataPagamento.HasValue
                ? l.DataPagamento.Value.ToString("dd/MM/yyyy")
                : (l.DataVencimento != default ? l.DataVencimento.ToString("dd/MM/yyyy") : "");

            var valor = l.Valor.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

            sb.AppendLine(string.Join(";", new[]
            {
                data,
                EscapeCsvField(l.Descricao),
                EscapeCsvField(l.PacienteNome ?? ""),
                EscapeCsvField(l.Categoria),
                EscapeCsvField(l.FormaPagamento ?? ""),
                valor,
                EscapeCsvField(l.Status)
            }));
        }

        // UTF-8 com BOM para Excel pt-BR abrir corretamente.
        var bom = System.Text.Encoding.UTF8.GetPreamble();
        var body = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + body.Length];
        bom.CopyTo(result, 0);
        body.CopyTo(result, bom.Length);
        return result;
    }

    private static string EscapeCsvField(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    [HttpGet("extrato")]
    [RequiresAcao("financeiro.ver")]
    public async Task<ActionResult<PaginaLancamentosExtratoDto>> ListarExtrato(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly dataFim,
        [FromQuery] string? tipo,
        [FromQuery] string? categoria,
        [FromQuery] string? formaPagamento,
        [FromQuery] string? origem,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10,
        // Modo vencidos (R4): quando true, ignora dataInicio/dataFim e lista
        // somente lançamentos Pendente com data_vencimento < hoje.
        [FromQuery] bool somenteVencidos = false)
    {
        var result = await _query.Query<ListarExtratoQuery, PaginaLancamentosExtratoDto>(
            new ListarExtratoQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim,
                Tipo = tipo,
                Categoria = categoria,
                FormaPagamento = formaPagamento,
                Origem = origem,
                Pagina = pagina,
                TamanhoPagina = tamanho,
                SomenteVencidos = somenteVencidos
            });
        return Ok(result);
    }

    // ─────────────────────────── F7 — Caixa Diário ─────────────────────────────

    [HttpGet("caixa")]
    [RequiresAcao("financeiro.ver")]
    public async Task<ActionResult<CaixaDiarioDto?>> ObterCaixa([FromQuery] DateOnly? data)
    {
        var result = await _query.Query<ObterCaixaDiarioQuery, CaixaDiarioDto?>(
            new ObterCaixaDiarioQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Data = data ?? BrasiliaTime.Today
            });
        return Ok(result);
    }

    [HttpPost("caixa/abrir")]
    [RequiresAcao("financeiro.fechar")]
    public async Task<ActionResult> AbrirCaixa([FromBody] AbrirCaixaDiarioDto dto)
    {
        await _cmd.Send(new AbrirCaixaDiarioCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Data = dto.Data ?? BrasiliaTime.Today,
            UsuarioId = _tenant.UsuarioId
        });
        return NoContent();
    }

    [HttpPost("caixa/fechar")]
    [RequiresAcao("financeiro.fechar")]
    public async Task<ActionResult> FecharCaixa([FromBody] FecharCaixaDiarioDto dto)
    {
        await _cmd.Send(new FecharCaixaDiarioCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Data = dto.Data ?? BrasiliaTime.Today,
            UsuarioId = _tenant.UsuarioId,
            Observacao = dto.Observacao
        });
        return NoContent();
    }

    [HttpPost("caixa/reabrir")]
    [RequiresAcao("financeiro.fechar")]
    public async Task<ActionResult> ReabrirCaixa([FromBody] ReabrirCaixaDiarioDto dto)
    {
        await _cmd.Send(new ReabrirCaixaDiarioCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Data = dto.Data ?? BrasiliaTime.Today,
            UsuarioId = _tenant.UsuarioId,
            EhDono = _tenant.EhDono
        });
        return NoContent();
    }

    // ─────────────────────────── F7 — Comissões ────────────────────────────────

    [HttpGet("comissoes")]
    [RequiresAcao("financeiro.ver")]
    public async Task<ActionResult<ComissaoPeriodoDto>> ObterComissoes(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly dataFim)
    {
        var result = await _query.Query<ObterComissoesPeriodoQuery, ComissaoPeriodoDto>(
            new ObterComissoesPeriodoQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                DataInicio = dataInicio,
                DataFim = dataFim
            });
        return Ok(result);
    }

    [HttpGet("comissoes/config/{profissionalUsuarioId:guid}")]
    [RequiresAcao("financeiro.ver")]
    public async Task<ActionResult<ConfigComissaoDto>> ObterConfigComissao(Guid profissionalUsuarioId)
    {
        var result = await _query.Query<ObterConfigComissaoQuery, ConfigComissaoDto>(
            new ObterConfigComissaoQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                ProfissionalUsuarioId = profissionalUsuarioId
            });
        return Ok(result);
    }

    [HttpPost("comissoes/config")]
    [RequiresAcao("financeiro.fechar")]
    public async Task<ActionResult> SalvarConfigComissao([FromBody] SalvarConfigComissaoProfissionalDto dto)
    {
        await _cmd.Send(new SalvarComissaoProfissionalCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ProfissionalUsuarioId = dto.ProfissionalUsuarioId,
            PercentualConsulta = dto.PercentualConsulta,
            PercentualProcedimento = dto.PercentualProcedimento,
            EhDono = _tenant.EhDono
        });
        return NoContent();
    }
}

public record CriarLancamentoDto(
    string Tipo,
    string Descricao,
    decimal Valor,
    DateOnly DataVencimento,
    string Categoria,
    long? OrcamentoId);

public record AtualizarLancamentoDto(
    string Descricao,
    decimal Valor,
    DateOnly DataVencimento,
    string Categoria);

public record PagarLancamentoDto(DateOnly? DataPagamento);

public record CriarCategoriaFinanceiraDto(string Nome, string Tipo);
public record AtualizarCategoriaFinanceiraDto(string Nome, string Tipo);
public record CriarFormaPagamentoDto(string Nome);
public record AtualizarFormaPagamentoDto(string Nome);

// F7 — Caixa Diário
public record AbrirCaixaDiarioDto(DateOnly? Data);
public record FecharCaixaDiarioDto(DateOnly? Data, string? Observacao);
public record ReabrirCaixaDiarioDto(DateOnly? Data);

// F7 — Comissão
public record SalvarConfigComissaoProfissionalDto(
    Guid ProfissionalUsuarioId,
    decimal? PercentualConsulta,
    decimal? PercentualProcedimento);
