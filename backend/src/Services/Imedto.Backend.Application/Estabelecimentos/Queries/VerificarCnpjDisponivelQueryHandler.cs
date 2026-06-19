using Imedto.Backend.Contracts.Estabelecimentos.Queries;
using Imedto.Backend.Domain.Inventario.Cadastros;
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
        var canônico = CnpjValidator.Normalizar(query.Cnpj);

        if (canônico is null || !CnpjValidator.EhValido(canônico))
            return new VerificarCnpjDisponivelResult(false, false, "CNPJ inválido.");

        if (await _queryRepository.ExisteCnpj(canônico))
            return new VerificarCnpjDisponivelResult(true, false, "CNPJ já cadastrado em outro estabelecimento.");

        return new VerificarCnpjDisponivelResult(true, true, null);
    }
}
