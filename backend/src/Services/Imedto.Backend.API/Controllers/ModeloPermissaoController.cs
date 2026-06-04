using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.ModelosPermissao.Commands;
using Imedto.Backend.Contracts.ModelosPermissao.Queries;
using Imedto.Backend.Contracts.ModelosPermissao.Queries.Results;
using Imedto.Backend.Contracts.Vinculos.Commands;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

[ApiController]
[Route("api/estabelecimento/modelos-permissao")]
[Authorize]
[RequiresEstabelecimento]
public class ModeloPermissaoController : ControllerBase
{
    private readonly ICommandBus _cmd;
    private readonly IRequestBus _query;
    private readonly ICurrentTenantAccessor _tenant;

    public ModeloPermissaoController(ICommandBus cmd, IRequestBus query, ICurrentTenantAccessor tenant)
    {
        _cmd = cmd;
        _query = query;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ModeloPermissaoDto>>> Listar()
    {
        var result = await _query.Query<ListarModelosPermissaoQuery, IEnumerable<ModeloPermissaoDto>>(
            new ListarModelosPermissaoQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(result);
    }

    [HttpPost]
    [RequiresPermissaoExtra(PermissoesExtras.GerirPermissoes)]
    public async Task<ActionResult> Criar([FromBody] CriarModeloPermissaoDto dto)
    {
        var cmd = new CriarModeloPermissaoCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            TipoAcesso = dto.TipoAcesso,
            Permissoes = dto.Permissoes ?? Array.Empty<string>(),
            Icone = dto.Icone,
            Cor = dto.Cor,
            Descricao = dto.Descricao,
        };
        await _cmd.Send(cmd);
        return Created($"api/estabelecimento/modelos-permissao", new { modeloId = cmd.ModeloIdCriado });
    }

    [HttpPut("{id:long}")]
    [RequiresPermissaoExtra(PermissoesExtras.GerirPermissoes)]
    public async Task<ActionResult> Atualizar(long id, [FromBody] AtualizarModeloPermissaoDto dto)
    {
        await _cmd.Send(new AtualizarModeloPermissaoCommand
        {
            ModeloId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Nome = dto.Nome,
            TipoAcesso = dto.TipoAcesso,
            Permissoes = dto.Permissoes ?? Array.Empty<string>(),
            Icone = dto.Icone,
            Cor = dto.Cor,
            Descricao = dto.Descricao,
        });
        return NoContent();
    }

    /// <summary>Exclui um modelo personalizado (modelos padrão e em uso são bloqueados).</summary>
    [HttpDelete("{id:long}")]
    [RequiresPapel(TenantPapel.Dono)]
    public async Task<ActionResult> Excluir(long id)
    {
        await _cmd.Send(new ExcluirModeloPermissaoCommand
        {
            ModeloId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
        });
        return NoContent();
    }

    /// <summary>Atribui (ou troca) o modelo de permissão de um vínculo profissional.</summary>
    [HttpPut("/api/estabelecimento/profissionais/{vinculoId:long}/modelo-permissao")]
    [RequiresPapel(TenantPapel.Dono)]
    public async Task<ActionResult> AtribuirAoVinculo(long vinculoId, [FromBody] AtribuirModeloAoVinculoDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _cmd.Send(new AlterarModeloPermissaoDoVinculoCommand
        {
            VinculoId = vinculoId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            NovoModeloPermissaoId = dto.ModeloPermissaoId,
            UsuarioSolicitanteId = userId,
        });

        return NoContent();
    }

    /// <summary>Define (ou limpa) a especialidade do vínculo para este estabelecimento.</summary>
    [HttpPut("/api/estabelecimento/profissionais/{vinculoId:long}/especialidade")]
    [RequiresPapel(TenantPapel.Dono)]
    public async Task<ActionResult> AlterarEspecialidadeDoVinculo(long vinculoId, [FromBody] AlterarEspecialidadeDoVinculoDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _cmd.Send(new AlterarEspecialidadeDoVinculoCommand
        {
            VinculoId = vinculoId,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            Especialidade = dto.Especialidade,
            UsuarioSolicitanteId = userId,
        });

        return NoContent();
    }
}

public record CriarModeloPermissaoDto(
    string Nome,
    string TipoAcesso,
    IReadOnlyList<string>? Permissoes,
    string? Icone = null,
    string? Cor = null,
    string? Descricao = null);

public record AtualizarModeloPermissaoDto(
    string Nome,
    string TipoAcesso,
    IReadOnlyList<string>? Permissoes,
    string? Icone = null,
    string? Cor = null,
    string? Descricao = null);

public record AtribuirModeloAoVinculoDto(long ModeloPermissaoId);

public record AlterarEspecialidadeDoVinculoDto(string? Especialidade);
