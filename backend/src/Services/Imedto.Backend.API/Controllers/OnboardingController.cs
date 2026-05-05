using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Imedto.Backend.Contracts.Onboarding.Commands;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Filters;

namespace Imedto.Backend.API.Controllers;

/// <summary>Finalização atômica do fluxo de onboarding.</summary>
[Authorize]
[ApiController]
[Route("api/onboarding")]
[Produces("application/json")]
public class OnboardingController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IMemoryCache _cache;

    public OnboardingController(ICommandBus commandBus, IMemoryCache cache)
    {
        _commandBus = commandBus;
        _cache = cache;
    }

    /// <summary>
    /// Finaliza o onboarding salvando todos os dados em uma única transação.
    /// A flag <c>onboarding_completo</c> só é marcada como <c>true</c> após todos
    /// os dados (perfil, estabelecimento, profissional, horários) serem salvos com sucesso.
    /// </summary>
    /// <response code="204">Onboarding concluído.</response>
    /// <response code="422">Dados inválidos ou CPF/CNPJ já cadastrado.</response>
    [AllowBeforeOnboarding]
    [HttpPost("finalizar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Finalizar([FromBody] FinalizarOnboardingRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        await _commandBus.Send(new FinalizarOnboardingCommand
        {
            NomeCompleto = request.NomeCompleto,
            Cpf = request.Cpf,
            Telefone = request.Telefone,
            Estabelecimento = request.Estabelecimento is null ? null : new EstabelecimentoOnboardingInput
            {
                NomeFantasia = request.Estabelecimento.NomeFantasia,
                Cnpj = request.Estabelecimento.Cnpj,
                Telefone = request.Estabelecimento.Telefone,
                Endereco = request.Estabelecimento.Endereco,
            },
            Profissional = request.Profissional is null ? null : new ProfissionalOnboardingInput
            {
                Conselho = request.Profissional.Conselho,
                Uf = request.Profissional.Uf,
                NumeroRegistro = request.Profissional.NumeroRegistro,
                Especialidade = request.Profissional.Especialidade,
            },
            Funcionamento = request.Funcionamento is null ? null : new FuncionamentoOnboardingInput
            {
                HorarioInicio = request.Funcionamento.HorarioInicio,
                HorarioFim = request.Funcionamento.HorarioFim,
                DuracaoConsultaPadraoMinutos = request.Funcionamento.DuracaoConsultaPadraoMinutos,
                IntervaloEntreConsultasMinutos = request.Funcionamento.IntervaloEntreConsultasMinutos,
                DiasSemana = request.Funcionamento.DiasSemana ?? [],
            },
        });

        // Invalida cache do filtro de onboarding para que as próximas chamadas passem imediatamente.
        _cache.Remove($"onboarding:{userId}");
        // E também o cache de /auth/me — onboardingCompleto, nome e telefone mudaram.
        _cache.Remove(AuthController.AuthMeCacheKey(userId));

        return NoContent();
    }
}

public record FinalizarOnboardingRequest(
    string NomeCompleto,
    string Cpf,
    string Telefone,
    EstabelecimentoOnboardingRequest? Estabelecimento,
    ProfissionalOnboardingRequest? Profissional,
    FuncionamentoOnboardingRequest? Funcionamento);

public record EstabelecimentoOnboardingRequest(
    string NomeFantasia,
    string? Cnpj,
    string? Telefone,
    string? Endereco);

public record ProfissionalOnboardingRequest(
    string Conselho,
    string Uf,
    string NumeroRegistro,
    string? Especialidade);

public record FuncionamentoOnboardingRequest(
    string HorarioInicio,
    string HorarioFim,
    int DuracaoConsultaPadraoMinutos,
    int IntervaloEntreConsultasMinutos,
    IReadOnlyList<int>? DiasSemana);
