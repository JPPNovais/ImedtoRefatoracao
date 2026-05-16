using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Inventario.Cadastros.Commands;
using Imedto.Backend.Contracts.Inventario.Cadastros.Queries;
using Imedto.Backend.Contracts.Inventario.Cadastros.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Cadastros mestre de estoque: categorias, fabricantes, fornecedores e locais.
///
/// • GET: qualquer usuário autenticado com a ação "estoque.ver" (mesma área da
///   tela de Inventário — reuso intencional).
/// • POST/PUT: apenas Dono ou Recepcionista — gestão de cadastros é função
///   administrativa. Profissional clínico não cria nem altera mestre.
/// • DELETE: idem POST/PUT. "Excluir" = inativar (preserva integridade referencial).
/// </summary>
[ApiController]
[Route("api/inventario/cadastros")]
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]
[RequiresAcao("estoque")]
public class CadastrosEstoqueController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public CadastrosEstoqueController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
    }

    // ═════════════════════════════ Categorias ═════════════════════════════

    [HttpGet("categorias")]
    public async Task<ActionResult<PaginaCategoriasEstoqueDto>> ListarCategorias(
        [FromQuery] string? busca,
        [FromQuery] bool? apenasAtivos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var res = await _query.Query<ListarCategoriasEstoqueQuery, PaginaCategoriasEstoqueDto>(
            new ListarCategoriasEstoqueQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Busca = busca,
                ApenasAtivos = apenasAtivos,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });
        return Ok(res);
    }

    [HttpGet("categorias/opcoes")]
    public async Task<ActionResult<IReadOnlyList<OpcaoCadastroEstoqueDto>>> ObterOpcoesCategorias()
    {
        var res = await _query.Query<ObterOpcoesCategoriasEstoqueQuery, IReadOnlyList<OpcaoCadastroEstoqueDto>>(
            new ObterOpcoesCategoriasEstoqueQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(res);
    }

    [HttpPost("categorias")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult<object>> CriarCategoria([FromBody] CategoriaPayloadDto dto)
    {
        var cmd = new CriarCategoriaEstoqueCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            Cor = dto.Cor,
            Icone = dto.Icone
        };
        await _cmd.Send(cmd);
        return CreatedAtAction(nameof(ListarCategorias), new { }, new { id = cmd.CategoriaIdCriada });
    }

    [HttpPut("categorias/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarCategoria(long id, [FromBody] CategoriaPayloadDto dto)
    {
        await _cmd.Send(new AtualizarCategoriaEstoqueCommand
        {
            CategoriaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            Cor = dto.Cor,
            Icone = dto.Icone
        });
        return NoContent();
    }

    [HttpPost("categorias/{id:long}/inativar")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> InativarCategoria(long id)
    {
        await _cmd.Send(new InativarCategoriaEstoqueCommand
        {
            CategoriaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    [HttpPost("categorias/{id:long}/reativar")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> ReativarCategoria(long id)
    {
        await _cmd.Send(new ReativarCategoriaEstoqueCommand
        {
            CategoriaId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    // ═════════════════════════════ Fabricantes ═════════════════════════════

    [HttpGet("fabricantes")]
    public async Task<ActionResult<PaginaFabricantesEstoqueDto>> ListarFabricantes(
        [FromQuery] string? busca,
        [FromQuery] bool? apenasAtivos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var res = await _query.Query<ListarFabricantesEstoqueQuery, PaginaFabricantesEstoqueDto>(
            new ListarFabricantesEstoqueQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Busca = busca,
                ApenasAtivos = apenasAtivos,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });
        return Ok(res);
    }

    [HttpGet("fabricantes/opcoes")]
    public async Task<ActionResult<IReadOnlyList<OpcaoCadastroEstoqueDto>>> ObterOpcoesFabricantes()
    {
        var res = await _query.Query<ObterOpcoesFabricantesEstoqueQuery, IReadOnlyList<OpcaoCadastroEstoqueDto>>(
            new ObterOpcoesFabricantesEstoqueQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(res);
    }

    [HttpPost("fabricantes")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult<object>> CriarFabricante([FromBody] FabricantePayloadDto dto)
    {
        var cmd = new CriarFabricanteEstoqueCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            Pais = dto.Pais
        };
        await _cmd.Send(cmd);
        return CreatedAtAction(nameof(ListarFabricantes), new { }, new { id = cmd.FabricanteIdCriado });
    }

    [HttpPut("fabricantes/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarFabricante(long id, [FromBody] FabricantePayloadDto dto)
    {
        await _cmd.Send(new AtualizarFabricanteEstoqueCommand
        {
            FabricanteId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            Pais = dto.Pais
        });
        return NoContent();
    }

    [HttpPost("fabricantes/{id:long}/inativar")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> InativarFabricante(long id)
    {
        await _cmd.Send(new InativarFabricanteEstoqueCommand
        {
            FabricanteId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    [HttpPost("fabricantes/{id:long}/reativar")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> ReativarFabricante(long id)
    {
        await _cmd.Send(new ReativarFabricanteEstoqueCommand
        {
            FabricanteId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    // ═════════════════════════════ Fornecedores ═════════════════════════════

    [HttpGet("fornecedores")]
    public async Task<ActionResult<PaginaFornecedoresEstoqueDto>> ListarFornecedores(
        [FromQuery] string? busca,
        [FromQuery] bool? apenasAtivos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var res = await _query.Query<ListarFornecedoresEstoqueQuery, PaginaFornecedoresEstoqueDto>(
            new ListarFornecedoresEstoqueQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Busca = busca,
                ApenasAtivos = apenasAtivos,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });
        return Ok(res);
    }

    [HttpGet("fornecedores/opcoes")]
    public async Task<ActionResult<IReadOnlyList<OpcaoCadastroEstoqueDto>>> ObterOpcoesFornecedores()
    {
        var res = await _query.Query<ObterOpcoesFornecedoresEstoqueQuery, IReadOnlyList<OpcaoCadastroEstoqueDto>>(
            new ObterOpcoesFornecedoresEstoqueQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(res);
    }

    [HttpPost("fornecedores")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult<object>> CriarFornecedor([FromBody] FornecedorPayloadDto dto)
    {
        var cmd = new CriarFornecedorEstoqueCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            RazaoSocial = dto.RazaoSocial,
            NomeFantasia = dto.NomeFantasia,
            Cnpj = dto.Cnpj,
            ContatoNome = dto.ContatoNome,
            ContatoTelefone = dto.ContatoTelefone,
            ContatoEmail = dto.ContatoEmail,
            PrazoEntregaDias = dto.PrazoEntregaDias
        };
        await _cmd.Send(cmd);
        return CreatedAtAction(nameof(ListarFornecedores), new { }, new { id = cmd.FornecedorIdCriado });
    }

    [HttpPut("fornecedores/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarFornecedor(long id, [FromBody] FornecedorPayloadDto dto)
    {
        await _cmd.Send(new AtualizarFornecedorEstoqueCommand
        {
            FornecedorId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            RazaoSocial = dto.RazaoSocial,
            NomeFantasia = dto.NomeFantasia,
            Cnpj = dto.Cnpj,
            ContatoNome = dto.ContatoNome,
            ContatoTelefone = dto.ContatoTelefone,
            ContatoEmail = dto.ContatoEmail,
            PrazoEntregaDias = dto.PrazoEntregaDias
        });
        return NoContent();
    }

    [HttpPost("fornecedores/{id:long}/inativar")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> InativarFornecedor(long id)
    {
        await _cmd.Send(new InativarFornecedorEstoqueCommand
        {
            FornecedorId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    [HttpPost("fornecedores/{id:long}/reativar")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> ReativarFornecedor(long id)
    {
        await _cmd.Send(new ReativarFornecedorEstoqueCommand
        {
            FornecedorId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    // ═════════════════════════════ Locais ═════════════════════════════

    [HttpGet("locais")]
    public async Task<ActionResult<PaginaLocaisEstoqueDto>> ListarLocais(
        [FromQuery] string? busca,
        [FromQuery] bool? apenasAtivos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20)
    {
        var res = await _query.Query<ListarLocaisEstoqueQuery, PaginaLocaisEstoqueDto>(
            new ListarLocaisEstoqueQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Busca = busca,
                ApenasAtivos = apenasAtivos,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });
        return Ok(res);
    }

    [HttpGet("locais/opcoes")]
    public async Task<ActionResult<IReadOnlyList<OpcaoCadastroEstoqueDto>>> ObterOpcoesLocais()
    {
        var res = await _query.Query<ObterOpcoesLocaisEstoqueQuery, IReadOnlyList<OpcaoCadastroEstoqueDto>>(
            new ObterOpcoesLocaisEstoqueQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(res);
    }

    [HttpPost("locais")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult<object>> CriarLocal([FromBody] LocalPayloadDto dto)
    {
        var cmd = new CriarLocalEstoqueCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            Tipo = dto.Tipo,
            AndarSetor = dto.AndarSetor,
            Responsavel = dto.Responsavel
        };
        await _cmd.Send(cmd);
        return CreatedAtAction(nameof(ListarLocais), new { }, new { id = cmd.LocalIdCriado });
    }

    [HttpPut("locais/{id:long}")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> AtualizarLocal(long id, [FromBody] LocalPayloadDto dto)
    {
        await _cmd.Send(new AtualizarLocalEstoqueCommand
        {
            LocalId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            Tipo = dto.Tipo,
            AndarSetor = dto.AndarSetor,
            Responsavel = dto.Responsavel
        });
        return NoContent();
    }

    [HttpPost("locais/{id:long}/inativar")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> InativarLocal(long id)
    {
        await _cmd.Send(new InativarLocalEstoqueCommand
        {
            LocalId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }

    [HttpPost("locais/{id:long}/reativar")]
    [RequiresPapel(TenantPapel.Dono, TenantPapel.Recepcionista)]
    public async Task<ActionResult> ReativarLocal(long id)
    {
        await _cmd.Send(new ReativarLocalEstoqueCommand
        {
            LocalId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId
        });
        return NoContent();
    }
}

// ─── DTOs públicos (input) ───────────────────────────────────────────────

public record CategoriaPayloadDto(string Nome, string Cor, string Icone);
public record FabricantePayloadDto(string Nome, string? Pais);
public record FornecedorPayloadDto(
    string RazaoSocial,
    string? NomeFantasia,
    string? Cnpj,
    string? ContatoNome,
    string? ContatoTelefone,
    string? ContatoEmail,
    int PrazoEntregaDias);
public record LocalPayloadDto(string Nome, string Tipo, string? AndarSetor, string? Responsavel);
