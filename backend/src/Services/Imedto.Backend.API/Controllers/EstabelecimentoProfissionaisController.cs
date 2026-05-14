using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Vinculos.Queries;
using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>Listagens relacionadas a vínculos: profissionais de um estabelecimento e convites do usuário.</summary>
[Authorize]
[ApiController]
[Produces("application/json")]
public class EstabelecimentoProfissionaisController : ControllerBase
{
    private readonly IRequestBus _requestBus;

    public EstabelecimentoProfissionaisController(IRequestBus requestBus)
    {
        _requestBus = requestBus;
    }

    /// <summary>
    /// Lista os profissionais do estabelecimento — DTO COMPLETO (com e-mail,
    /// modelo de permissão, datas de convite/aceite e status Inativo/Convidado).
    /// Acesso restrito a Dono ou a quem tem <c>equipe.ver</c> no modelo de
    /// permissão — antes esse endpoint vazava e-mails/permissões/datas a
    /// QUALQUER membro do tenant (LGPD: violação de minimização e do
    /// princípio de necessidade de acesso).
    ///
    /// Para seletores (agenda/prontuário/orçamento) que só precisam de
    /// nome + especialidade, use o endpoint público
    /// <c>GET /api/estabelecimento/{id}/profissionais/publico</c>.
    ///
    /// Operações de escrita (convidar, inativar, trocar modelo) continuam
    /// Dono-only em seus próprios handlers — defense-in-depth.
    /// </summary>
    [HttpGet("/api/estabelecimento/{estabelecimentoId:long}/profissionais")]
    [RequiresEstabelecimento]
    [RequiresAcao("equipe", "ver")]
    [ProducesResponseType(typeof(IEnumerable<ProfissionalVinculadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ListarProfissionais(long estabelecimentoId, [FromQuery] bool incluirInativos = false)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        var resultado = await _requestBus.Query<ListarProfissionaisEstabelecimentoQuery, IEnumerable<ProfissionalVinculadoDto>>(
            new ListarProfissionaisEstabelecimentoQuery
            {
                EstabelecimentoId = estabelecimentoId,
                UsuarioSolicitanteId = userId,
                IncluirInativos = incluirInativos
            });

        return Ok(resultado);
    }

    /// <summary>
    /// Lista pública/minimizada de profissionais — só Ativos + Dono, com
    /// nome, especialidade, conselho e status. Sem e-mail, sem datas, sem
    /// modelo de permissão. Acessível a qualquer membro ativo do tenant
    /// (gate via <c>[RequiresEstabelecimento]</c>).
    ///
    /// Usado pelo front em seletores de agenda/prontuário/orçamento — onde
    /// a UX só pede "com qual profissional?", sem expor PII da equipe.
    /// </summary>
    [HttpGet("/api/estabelecimento/{estabelecimentoId:long}/profissionais/publico")]
    [RequiresEstabelecimento]
    [ProducesResponseType(typeof(IEnumerable<ProfissionalPublicoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarProfissionaisPublico(long estabelecimentoId)
    {
        var resultado = await _requestBus.Query<ListarProfissionaisPublicoQuery, IEnumerable<ProfissionalPublicoDto>>(
            new ListarProfissionaisPublicoQuery
            {
                EstabelecimentoId = estabelecimentoId
            });

        return Ok(resultado);
    }

    /// <summary>Usuário lista seus convites pendentes.</summary>
    [AllowBeforeOnboarding]
    [HttpGet("/api/vinculo/convites/me")]
    [ProducesResponseType(typeof(IEnumerable<ConviteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarMeusConvites()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        var resultado = await _requestBus.Query<ListarMeusConvitesQuery, IEnumerable<ConviteDto>>(
            new ListarMeusConvitesQuery { UsuarioId = userId });

        return Ok(resultado);
    }
}
