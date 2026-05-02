using Imedto.Backend.Contracts.Usuarios.Queries;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Usuarios.Queries;

/// <summary>
/// Validação de CPF no padrão brasileiro (dígitos verificadores) +
/// checagem de duplicidade (ignorando o próprio usuário corrente).
/// </summary>
public class VerificarCpfDisponivelQueryHandler
    : IRequestHandler<VerificarCpfDisponivelQuery, VerificarCpfDisponivelResult>
{
    private readonly UsuarioQueryRepository _queryRepository;

    public VerificarCpfDisponivelQueryHandler(UsuarioQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<VerificarCpfDisponivelResult> Handle(VerificarCpfDisponivelQuery query)
    {
        var digitos = new string((query.Cpf ?? "").Where(char.IsDigit).ToArray());

        if (digitos.Length != 11 || !DigitosVerificadoresValidos(digitos))
            return new VerificarCpfDisponivelResult(false, false, "CPF inválido.");

        var ocupado = await _queryRepository.ExisteCpfEmOutroUsuario(digitos, query.UsuarioId);
        if (ocupado)
            return new VerificarCpfDisponivelResult(true, false, "CPF já cadastrado em outra conta.");

        return new VerificarCpfDisponivelResult(true, true, null);
    }

    /// <summary>
    /// Algoritmo padrão de validação de CPF: rejeita sequências repetidas
    /// (000.000.000-00 etc.) e confere ambos os dígitos verificadores.
    /// </summary>
    private static bool DigitosVerificadoresValidos(string digitos)
    {
        if (digitos.Distinct().Count() == 1) return false;

        var nums = digitos.Select(c => c - '0').ToArray();

        var soma1 = 0;
        for (var i = 0; i < 9; i++) soma1 += nums[i] * (10 - i);
        var dv1 = (soma1 * 10) % 11;
        if (dv1 == 10) dv1 = 0;
        if (dv1 != nums[9]) return false;

        var soma2 = 0;
        for (var i = 0; i < 10; i++) soma2 += nums[i] * (11 - i);
        var dv2 = (soma2 * 10) % 11;
        if (dv2 == 10) dv2 = 0;
        return dv2 == nums[10];
    }
}
