using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Pacientes.Queries;

/// <summary>
/// Handler de leitura de paciente. <b>Scoped</b> (nao Singleton) porque
/// audita acesso via <see cref="IPacienteAcessoLogService"/> (LGPD).
/// </summary>
public class ObterPacienteQueryHandlers : IRequestHandler<ObterPacienteQuery, PacienteDto>
{
    private readonly PacienteQueryRepository _queryRepository;
    private readonly IPacienteAcessoLogService _acessoLog;

    public ObterPacienteQueryHandlers(
        PacienteQueryRepository queryRepository,
        IPacienteAcessoLogService acessoLog)
    {
        _queryRepository = queryRepository;
        _acessoLog = acessoLog;
    }

    public async Task<PacienteDto> Handle(ObterPacienteQuery query)
    {
        var dto = await _queryRepository.ObterPorId(query.PacienteId, query.EstabelecimentoId);
        if (dto is null) return null;

        // Audit LGPD: registrar acesso de leitura aos dados pessoais. Falha de
        // gravacao do log nao quebra o fluxo (best-effort no service).
        await _acessoLog.RegistrarAsync(
            query.PacienteId, query.SolicitanteUsuarioId, query.EstabelecimentoId, TipoAcessoPaciente.Leitura);

        if (query.MascararContato)
            AplicarMascaraContato(dto);

        return dto;
    }

    // Mascaramento na borda: substitui CPF e telefone por strings com caracteres
    // ofuscadores, preservando os últimos dígitos para identificação visual no mobile.
    // O aggregate não é tocado — PII completa nunca sai do banco para o payload mobile.
    private static void AplicarMascaraContato(PacienteDto dto)
    {
        dto.Cpf = MascararCpf(dto.Cpf);
        dto.Telefone = MascararTelefone(dto.Telefone);
    }

    internal static string? MascararCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return cpf;

        // Remove não-dígitos para extrair os últimos 2 dígitos (pós-hífen).
        var digitos = new string(cpf.Where(char.IsDigit).ToArray());
        var ultimos2 = digitos.Length >= 2 ? digitos[^2..] : digitos;
        return $"•••.•••.•••-{ultimos2}";
    }

    internal static string? MascararTelefone(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone)) return telefone;

        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        var ultimos4 = digitos.Length >= 4 ? digitos[^4..] : digitos;
        // Detecta celular (11 dígitos com DDD) vs fixo (10 dígitos).
        return digitos.Length >= 11
            ? $"(••) •••••-{ultimos4}"
            : $"(••) ••••-{ultimos4}";
    }
}
