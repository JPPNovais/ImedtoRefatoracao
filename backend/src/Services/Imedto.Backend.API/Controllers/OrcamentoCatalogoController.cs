using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Commands;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries;
using Imedto.Backend.Contracts.Orcamentos.Catalogos.Queries.Results;
using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/orcamentos/configuracoes")]
[Authorize]
[RequiresEstabelecimento]
[FeatureGate(Features.OrcamentoCompleto)]
public class OrcamentoCatalogoController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public OrcamentoCatalogoController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd; _query = query; _tenant = tenant;
    }

    // ─── Cirurgias / Procedimentos ───
    [HttpGet("cirurgias")]
    [HttpGet("procedimentos")]
    public async Task<ActionResult<IEnumerable<CatalogoCirurgiaDto>>> ListarCirurgias([FromQuery] bool? ativas)
    {
        var data = await _query.Query<ListarCatalogoCirurgiasQuery, IEnumerable<CatalogoCirurgiaDto>>(
            new ListarCatalogoCirurgiasQuery { EstabelecimentoId = _tenant.EstabelecimentoId, Ativas = ativas });
        return Ok(data);
    }

    [HttpPost("cirurgias")]
    [HttpPost("procedimentos")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> CriarCirurgia([FromBody] CriarCirurgiaDto dto)
    {
        var cmd = new CriarCatalogoCirurgiaCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Descricao = dto.Descricao, ValorBase = dto.ValorBase, DuracaoPadraoMinutos = dto.DuracaoPadraoMinutos,
            CodigoInterno = dto.CodigoInterno, CodigoTuss = dto.CodigoTuss, Categoria = dto.Categoria
        };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpPut("cirurgias/{id:long}")]
    [HttpPut("procedimentos/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarCirurgia(long id, [FromBody] CriarCirurgiaDto dto)
    {
        await _cmd.Send(new AtualizarCatalogoCirurgiaCommand
        {
            Id = id, EstabelecimentoId = _tenant.EstabelecimentoId,
            Descricao = dto.Descricao, ValorBase = dto.ValorBase, DuracaoPadraoMinutos = dto.DuracaoPadraoMinutos,
            CodigoInterno = dto.CodigoInterno, CodigoTuss = dto.CodigoTuss, Categoria = dto.Categoria
        });
        return NoContent();
    }

    [HttpDelete("cirurgias/{id:long}")]
    [HttpDelete("procedimentos/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> RemoverCirurgia(long id)
    {
        await _cmd.Send(new RemoverCatalogoCirurgiaCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        return NoContent();
    }

    // ─── Valor profissional (legado) ───
    [HttpGet("valores-profissional")]
    public async Task<ActionResult<IEnumerable<ValorProfissionalOrcamentoDto>>> ListarValores([FromQuery] bool? ativos)
    {
        var data = await _query.Query<ListarValoresProfissionalQuery, IEnumerable<ValorProfissionalOrcamentoDto>>(
            new ListarValoresProfissionalQuery { EstabelecimentoId = _tenant.EstabelecimentoId, Ativos = ativos });
        return Ok(data);
    }

    [HttpPost("valores-profissional")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> CriarValor([FromBody] CriarValorProfissionalDto dto)
    {
        var cmd = new CriarValorProfissionalCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            ProfissionalUsuarioId = dto.ProfissionalUsuarioId, Funcao = dto.Funcao,
            TempoBaseMinutos = dto.TempoBaseMinutos, ValorTempoBase = dto.ValorTempoBase,
            TempoAdicionalMinutos = dto.TempoAdicionalMinutos, ValorAdicional = dto.ValorAdicional,
            ValorPlus = dto.ValorPlus
        };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpPut("valores-profissional/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarValor(long id, [FromBody] AtualizarValorProfissionalDto dto)
    {
        await _cmd.Send(new AtualizarValorProfissionalCommand
        {
            Id = id, EstabelecimentoId = _tenant.EstabelecimentoId, Funcao = dto.Funcao,
            TempoBaseMinutos = dto.TempoBaseMinutos, ValorTempoBase = dto.ValorTempoBase,
            TempoAdicionalMinutos = dto.TempoAdicionalMinutos, ValorAdicional = dto.ValorAdicional, ValorPlus = dto.ValorPlus
        });
        return NoContent();
    }

    [HttpDelete("valores-profissional/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> RemoverValor(long id)
    {
        await _cmd.Send(new RemoverValorProfissionalCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        return NoContent();
    }

    // ─── Local cirurgia ───
    [HttpGet("local-cirurgia")]
    public async Task<ActionResult<IEnumerable<ConfiguracaoLocalCirurgiaDto>>> ListarLocais()
    {
        var data = await _query.Query<ListarConfiguracoesLocalQuery, IEnumerable<ConfiguracaoLocalCirurgiaDto>>(
            new ListarConfiguracoesLocalQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(data);
    }

    [HttpPut("local-cirurgia/{tipo}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> SalvarLocal(string tipo, [FromBody] SalvarLocalDto dto)
    {
        var cmd = new SalvarConfiguracaoLocalCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId, TipoInternacao = tipo,
            TempoBaseMinutos = dto.TempoBaseMinutos, ValorBase = dto.ValorBase,
            TempoAdicionalMinutos = dto.TempoAdicionalMinutos, ValorAdicional = dto.ValorAdicional
        };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdSalvo });
    }

    // ─── Equipes legado ───
    [HttpGet("equipes")]
    public async Task<ActionResult<IEnumerable<CatalogoEquipeEspecializadaDto>>> ListarEquipes([FromQuery] bool? ativas)
    {
        var data = await _query.Query<ListarCatalogoEquipesQuery, IEnumerable<CatalogoEquipeEspecializadaDto>>(
            new ListarCatalogoEquipesQuery { EstabelecimentoId = _tenant.EstabelecimentoId, Ativas = ativas });
        return Ok(data);
    }

    [HttpPost("equipes")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> CriarEquipe([FromBody] CriarEquipeDto dto)
    {
        var cmd = new CriarCatalogoEquipeCommand { EstabelecimentoId = _tenant.EstabelecimentoId, Descricao = dto.Descricao, ValorPadrao = dto.ValorPadrao };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpPut("equipes/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarEquipe(long id, [FromBody] CriarEquipeDto dto)
    {
        await _cmd.Send(new AtualizarCatalogoEquipeCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId, Descricao = dto.Descricao, ValorPadrao = dto.ValorPadrao });
        return NoContent();
    }

    [HttpDelete("equipes/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> RemoverEquipe(long id)
    {
        await _cmd.Send(new RemoverCatalogoEquipeCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        return NoContent();
    }

    // ─── Implantes ───
    [HttpGet("implantes")]
    public async Task<ActionResult<IEnumerable<CatalogoImplanteDto>>> ListarImplantes([FromQuery] bool? ativos)
    {
        var data = await _query.Query<ListarCatalogoImplantesQuery, IEnumerable<CatalogoImplanteDto>>(
            new ListarCatalogoImplantesQuery { EstabelecimentoId = _tenant.EstabelecimentoId, Ativos = ativos });
        return Ok(data);
    }

    [HttpPost("implantes")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> CriarImplante([FromBody] CriarImplanteDto dto)
    {
        var cmd = new CriarCatalogoImplanteCommand { EstabelecimentoId = _tenant.EstabelecimentoId, ItemInventarioId = dto.ItemInventarioId, Descricao = dto.Descricao, CustoUnitario = dto.CustoUnitario };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpPut("implantes/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarImplante(long id, [FromBody] CriarImplanteDto dto)
    {
        await _cmd.Send(new AtualizarCatalogoImplanteCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId, ItemInventarioId = dto.ItemInventarioId, Descricao = dto.Descricao, CustoUnitario = dto.CustoUnitario });
        return NoContent();
    }

    [HttpDelete("implantes/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> RemoverImplante(long id)
    {
        await _cmd.Send(new RemoverCatalogoImplanteCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        return NoContent();
    }

    // ─── Pagamento ───
    [HttpGet("pagamento")]
    public async Task<ActionResult<IEnumerable<ConfiguracaoPagamentoCatalogoDto>>> ListarPagamentos([FromQuery] bool? ativas)
    {
        var data = await _query.Query<ListarConfiguracoesPagamentoQuery, IEnumerable<ConfiguracaoPagamentoCatalogoDto>>(
            new ListarConfiguracoesPagamentoQuery { EstabelecimentoId = _tenant.EstabelecimentoId, Ativas = ativas });
        return Ok(data);
    }

    [HttpPost("pagamento")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> CriarPagamento([FromBody] CriarConfigPagamentoDto dto)
    {
        var cmd = new CriarConfiguracaoPagamentoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId, FormaPagamentoId = dto.FormaPagamentoId,
            AcrescimoPercentual = dto.AcrescimoPercentual, EntradaPercentualPadrao = dto.EntradaPercentualPadrao,
            TaxaParcela = dto.TaxaParcela, ParcelasMaximas = dto.ParcelasMaximas
        };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpPut("pagamento/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarPagamento(long id, [FromBody] AtualizarConfigPagamentoDto dto)
    {
        await _cmd.Send(new AtualizarConfiguracaoPagamentoCommand
        {
            Id = id, EstabelecimentoId = _tenant.EstabelecimentoId,
            AcrescimoPercentual = dto.AcrescimoPercentual, EntradaPercentualPadrao = dto.EntradaPercentualPadrao,
            TaxaParcela = dto.TaxaParcela, ParcelasMaximas = dto.ParcelasMaximas
        });
        return NoContent();
    }

    [HttpDelete("pagamento/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> RemoverPagamento(long id)
    {
        await _cmd.Send(new RemoverConfiguracaoPagamentoCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        return NoContent();
    }

    // ─── Produtos ───
    [HttpGet("produtos")]
    public async Task<ActionResult<IEnumerable<CatalogoProdutoDto>>> ListarProdutos([FromQuery] bool? ativos)
    {
        var data = await _query.Query<ListarCatalogoProdutosQuery, IEnumerable<CatalogoProdutoDto>>(
            new ListarCatalogoProdutosQuery { EstabelecimentoId = _tenant.EstabelecimentoId, Ativos = ativos });
        return Ok(data);
    }

    [HttpPost("produtos")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> CriarProduto([FromBody] CriarProdutoDto dto)
    {
        var cmd = new CriarCatalogoProdutoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId, Nome = dto.Nome, Descricao = dto.Descricao,
            ValorReferencia = dto.ValorReferencia, UsoUnico = dto.UsoUnico,
            Tipo = dto.Tipo, Marca = dto.Marca, Unidade = dto.Unidade,
            FornecedorNome = dto.FornecedorNome, CodigoSku = dto.CodigoSku
        };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpPut("produtos/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarProduto(long id, [FromBody] CriarProdutoDto dto)
    {
        await _cmd.Send(new AtualizarCatalogoProdutoCommand
        {
            Id = id, EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome, Descricao = dto.Descricao, ValorReferencia = dto.ValorReferencia, UsoUnico = dto.UsoUnico,
            Tipo = dto.Tipo, Marca = dto.Marca, Unidade = dto.Unidade,
            FornecedorNome = dto.FornecedorNome, CodigoSku = dto.CodigoSku
        });
        return NoContent();
    }

    [HttpDelete("produtos/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> RemoverProduto(long id)
    {
        await _cmd.Send(new RemoverCatalogoProdutoCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        return NoContent();
    }

    // ─── Vínculo cirurgia × produto ───
    [HttpGet("cirurgias/{cirurgiaId:long}/produtos")]
    [HttpGet("procedimentos/{cirurgiaId:long}/produtos")]
    public async Task<ActionResult<IEnumerable<CatalogoCirurgiaProdutoDto>>> ListarProdutosDaCirurgia(long cirurgiaId)
    {
        var data = await _query.Query<ListarProdutosDaCirurgiaQuery, IEnumerable<CatalogoCirurgiaProdutoDto>>(
            new ListarProdutosDaCirurgiaQuery { CatalogoCirurgiaId = cirurgiaId, EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(data);
    }

    [HttpPost("cirurgias/{cirurgiaId:long}/produtos")]
    [HttpPost("procedimentos/{cirurgiaId:long}/produtos")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> VincularProdutoCirurgia(long cirurgiaId, [FromBody] VincularProdutoDto dto)
    {
        var cmd = new VincularProdutoCirurgiaCommand
        {
            CatalogoCirurgiaId = cirurgiaId, CatalogoProdutoId = dto.ProdutoId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            QuantidadePadrao = dto.QuantidadePadrao, Obrigatorio = dto.Obrigatorio,
            Incluido = dto.Incluido ?? true
        };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpPut("cirurgias/produtos/{vinculoId:long}")]
    [HttpPut("procedimentos/produtos/{vinculoId:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarVinculo(long vinculoId, [FromBody] AtualizarVinculoProdutoDto dto)
    {
        await _cmd.Send(new AtualizarVinculoProdutoCirurgiaCommand
        {
            VinculoId = vinculoId, EstabelecimentoId = _tenant.EstabelecimentoId,
            QuantidadePadrao = dto.QuantidadePadrao, Obrigatorio = dto.Obrigatorio,
            Incluido = dto.Incluido ?? true
        });
        return NoContent();
    }

    [HttpDelete("cirurgias/produtos/{vinculoId:long}")]
    [HttpDelete("procedimentos/produtos/{vinculoId:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> Desvincular(long vinculoId)
    {
        await _cmd.Send(new DesvincularProdutoCirurgiaCommand { VinculoId = vinculoId, EstabelecimentoId = _tenant.EstabelecimentoId });
        return NoContent();
    }

    // ─── Team Roles ───
    [HttpGet("team-roles")]
    public async Task<ActionResult<IEnumerable<OrcamentoTeamRoleDto>>> ListarTeamRoles([FromQuery] bool? ativos)
    {
        var data = await _query.Query<ListarOrcamentoTeamRolesQuery, IEnumerable<OrcamentoTeamRoleDto>>(
            new ListarOrcamentoTeamRolesQuery { EstabelecimentoId = _tenant.EstabelecimentoId, Ativos = ativos });
        return Ok(data);
    }

    [HttpPost("team-roles")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> CriarTeamRole([FromBody] CriarTeamRoleDto dto)
    {
        var cmd = new CriarOrcamentoTeamRoleCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId, Papel = dto.Papel,
            ProfissionalUsuarioId = dto.ProfissionalUsuarioId, NomePadrao = dto.NomePadrao,
            TipoHonorario = dto.TipoHonorario, Valor = dto.Valor, BaseCalculo = dto.BaseCalculo
        };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpPut("team-roles/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarTeamRole(long id, [FromBody] CriarTeamRoleDto dto)
    {
        await _cmd.Send(new AtualizarOrcamentoTeamRoleCommand
        {
            Id = id, EstabelecimentoId = _tenant.EstabelecimentoId, Papel = dto.Papel,
            ProfissionalUsuarioId = dto.ProfissionalUsuarioId, NomePadrao = dto.NomePadrao,
            TipoHonorario = dto.TipoHonorario, Valor = dto.Valor, BaseCalculo = dto.BaseCalculo
        });
        return NoContent();
    }

    [HttpDelete("team-roles/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> RemoverTeamRole(long id)
    {
        await _cmd.Send(new RemoverOrcamentoTeamRoleCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        return NoContent();
    }

    // ─── Anestesistas ───
    [HttpGet("anestesistas")]
    public async Task<ActionResult<IEnumerable<OrcamentoAnestesistaDto>>> ListarAnestesistas([FromQuery] bool? ativos)
    {
        var data = await _query.Query<ListarOrcamentoAnestesistasQuery, IEnumerable<OrcamentoAnestesistaDto>>(
            new ListarOrcamentoAnestesistasQuery { EstabelecimentoId = _tenant.EstabelecimentoId, Ativos = ativos });
        return Ok(data);
    }

    [HttpGet("anestesistas/{id:long}")]
    public async Task<ActionResult<OrcamentoAnestesistaDto>> ObterAnestesista(long id)
    {
        var data = await _query.Query<ObterOrcamentoAnestesistaQuery, OrcamentoAnestesistaDto?>(
            new ObterOrcamentoAnestesistaQuery { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        if (data is null) return NotFound(new { mensagem = "Anestesista não encontrado." });
        return Ok(data);
    }

    [HttpPost("anestesistas")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> CriarAnestesista([FromBody] CriarAnestesistaDto dto)
    {
        var cmd = new CriarOrcamentoAnestesistaCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId, Nome = dto.Nome,
            ProfissionalUsuarioId = dto.ProfissionalUsuarioId, Crm = dto.Crm,
            Especialidade = dto.Especialidade, Telefone = dto.Telefone, TabelaHonorarios = dto.TabelaHonorarios,
            Faixas = (dto.Faixas ?? new()).Select(f => new FaixaAnestesistaInput { Descricao = f.Descricao, Valor = f.Valor }).ToList()
        };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpPut("anestesistas/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarAnestesista(long id, [FromBody] CriarAnestesistaDto dto)
    {
        await _cmd.Send(new AtualizarOrcamentoAnestesistaCommand
        {
            Id = id, EstabelecimentoId = _tenant.EstabelecimentoId, Nome = dto.Nome,
            ProfissionalUsuarioId = dto.ProfissionalUsuarioId, Crm = dto.Crm,
            Especialidade = dto.Especialidade, Telefone = dto.Telefone, TabelaHonorarios = dto.TabelaHonorarios,
            Faixas = (dto.Faixas ?? new()).Select(f => new FaixaAnestesistaInput { Descricao = f.Descricao, Valor = f.Valor }).ToList()
        });
        return NoContent();
    }

    [HttpDelete("anestesistas/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> RemoverAnestesista(long id)
    {
        await _cmd.Send(new RemoverOrcamentoAnestesistaCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        return NoContent();
    }

    // ─── Pacotes ───
    [HttpGet("pacotes")]
    public async Task<ActionResult<IEnumerable<OrcamentoPacoteResumoDto>>> ListarPacotes([FromQuery] bool? ativos)
    {
        var data = await _query.Query<ListarOrcamentoPacotesQuery, IEnumerable<OrcamentoPacoteResumoDto>>(
            new ListarOrcamentoPacotesQuery { EstabelecimentoId = _tenant.EstabelecimentoId, Ativos = ativos });
        return Ok(data);
    }

    [HttpGet("pacotes/{id:long}")]
    public async Task<ActionResult<OrcamentoPacoteDetalheDto>> ObterPacote(long id)
    {
        var data = await _query.Query<ObterOrcamentoPacoteQuery, OrcamentoPacoteDetalheDto?>(
            new ObterOrcamentoPacoteQuery { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        if (data is null) return NotFound(new { mensagem = "Pacote não encontrado." });
        return Ok(data);
    }

    [HttpPost("pacotes")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> CriarPacote([FromBody] CriarPacoteDto dto)
    {
        var cmd = new CriarOrcamentoPacoteCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId, Nome = dto.Nome, Descricao = dto.Descricao,
            AnestesistaId = dto.AnestesistaId, ValorTotalSugerido = dto.ValorTotalSugerido,
            ProcedimentoIds = dto.ProcedimentoIds ?? new(),
            Produtos = (dto.Produtos ?? new()).Select(p => new ProdutoDoPacoteInput { ProdutoId = p.ProdutoId, Quantidade = p.Quantidade }).ToList(),
            TeamRoleIds = dto.TeamRoleIds ?? new()
        };
        await _cmd.Send(cmd);
        return Ok(new { id = cmd.IdCriado });
    }

    [HttpPut("pacotes/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarPacote(long id, [FromBody] CriarPacoteDto dto)
    {
        await _cmd.Send(new AtualizarOrcamentoPacoteCommand
        {
            Id = id, EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome, Descricao = dto.Descricao,
            AnestesistaId = dto.AnestesistaId, ValorTotalSugerido = dto.ValorTotalSugerido,
            ProcedimentoIds = dto.ProcedimentoIds ?? new(),
            Produtos = (dto.Produtos ?? new()).Select(p => new ProdutoDoPacoteInput { ProdutoId = p.ProdutoId, Quantidade = p.Quantidade }).ToList(),
            TeamRoleIds = dto.TeamRoleIds ?? new()
        });
        return NoContent();
    }

    [HttpDelete("pacotes/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> RemoverPacote(long id)
    {
        await _cmd.Send(new RemoverOrcamentoPacoteCommand { Id = id, EstabelecimentoId = _tenant.EstabelecimentoId });
        return NoContent();
    }
}

public record CriarCirurgiaDto(string Descricao, decimal ValorBase, int? DuracaoPadraoMinutos,
    string? CodigoInterno = null, string? CodigoTuss = null, string? Categoria = null);
public record CriarValorProfissionalDto(Guid? ProfissionalUsuarioId, string Funcao,
    int TempoBaseMinutos, decimal ValorTempoBase, int TempoAdicionalMinutos, decimal ValorAdicional, decimal ValorPlus);
public record AtualizarValorProfissionalDto(string Funcao,
    int TempoBaseMinutos, decimal ValorTempoBase, int TempoAdicionalMinutos, decimal ValorAdicional, decimal ValorPlus);
public record SalvarLocalDto(int TempoBaseMinutos, decimal ValorBase, int TempoAdicionalMinutos, decimal ValorAdicional);
public record CriarEquipeDto(string Descricao, decimal ValorPadrao);
public record CriarImplanteDto(long? ItemInventarioId, string Descricao, decimal CustoUnitario);
public record CriarConfigPagamentoDto(long FormaPagamentoId, decimal AcrescimoPercentual, decimal EntradaPercentualPadrao, decimal TaxaParcela, int ParcelasMaximas);
public record AtualizarConfigPagamentoDto(decimal AcrescimoPercentual, decimal EntradaPercentualPadrao, decimal TaxaParcela, int ParcelasMaximas);
public record CriarProdutoDto(string Nome, string? Descricao, decimal? ValorReferencia, bool UsoUnico,
    string? Tipo = null, string? Marca = null, string? Unidade = null, string? FornecedorNome = null, string? CodigoSku = null);
public record VincularProdutoDto(long ProdutoId, decimal QuantidadePadrao, bool Obrigatorio, bool? Incluido = null);
public record AtualizarVinculoProdutoDto(decimal QuantidadePadrao, bool Obrigatorio, bool? Incluido = null);
public record CriarTeamRoleDto(string Papel, Guid? ProfissionalUsuarioId, string? NomePadrao,
    string TipoHonorario, decimal Valor, string BaseCalculo);
public record FaixaAnestesistaDto(string Descricao, decimal Valor);
public record CriarAnestesistaDto(string Nome, Guid? ProfissionalUsuarioId, string? Crm,
    string? Especialidade, string? Telefone, string? TabelaHonorarios, List<FaixaAnestesistaDto>? Faixas);
public record ProdutoPacoteDto(long ProdutoId, decimal Quantidade);
public record CriarPacoteDto(string Nome, string? Descricao, long? AnestesistaId, decimal? ValorTotalSugerido,
    List<long>? ProcedimentoIds, List<ProdutoPacoteDto>? Produtos, List<long>? TeamRoleIds);
