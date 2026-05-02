using Imedto.Backend.Contracts.Estabelecimentos.Queries;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Application.Estabelecimentos.Queries;

/// <summary>
/// Validação completa de CNPJ: dígitos verificadores + checagem de duplicidade.
/// </summary>
public class VerificarCnpjDisponivelQueryHandler
    : IRequestHandler<VerificarCnpjDisponivelQuery, VerificarCnpjDisponivelResult>
{
    private readonly EstabelecimentoQueryRepository _queryRepository;

    public VerificarCnpjDisponivelQueryHandler(EstabelecimentoQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<VerificarCnpjDisponivelResult> Handle(VerificarCnpjDisponivelQuery query)
    {
        var digitos = new string((query.Cnpj ?? "").Where(char.IsDigit).ToArray());

        if (digitos.Length != 14 || !DigitosVerificadoresValidos(digitos))
            return new VerificarCnpjDisponivelResult(false, false, "CNPJ inválido.");

        if (await _queryRepository.ExisteCnpj(digitos))
            return new VerificarCnpjDisponivelResult(true, false, "CNPJ já cadastrado em outro estabelecimento.");

        return new VerificarCnpjDisponivelResult(true, true, null);
    }

    /// <summary>
    /// Algoritmo padrão de validação de CNPJ: rejeita sequências repetidas
    /// e confere ambos os dígitos verificadores.
    /// </summary>
    private static bool DigitosVerificadoresValidos(string digitos)
    {
        if (digitos.Distinct().Count() == 1) return false;

        var nums = digitos.Select(c => c - '0').ToArray();

        // Primeiro DV: pesos 5,4,3,2,9,8,7,6,5,4,3,2 sobre os 12 primeiros dígitos.
        var pesos1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var soma1 = 0;
        for (var i = 0; i < 12; i++) soma1 += nums[i] * pesos1[i];
        var dv1 = soma1 % 11;
        dv1 = dv1 < 2 ? 0 : 11 - dv1;
        if (dv1 != nums[12]) return false;

        // Segundo DV: pesos 6,5,4,3,2,9,8,7,6,5,4,3,2 sobre os 13 primeiros dígitos.
        var pesos2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var soma2 = 0;
        for (var i = 0; i < 13; i++) soma2 += nums[i] * pesos2[i];
        var dv2 = soma2 % 11;
        dv2 = dv2 < 2 ? 0 : 11 - dv2;
        return dv2 == nums[13];
    }
}
