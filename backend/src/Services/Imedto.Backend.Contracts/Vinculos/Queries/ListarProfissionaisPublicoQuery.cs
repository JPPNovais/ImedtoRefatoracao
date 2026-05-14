using Imedto.Backend.Contracts.Vinculos.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Queries;

/// <summary>
/// Versão pública da listagem de profissionais — retorna o DTO minimizado
/// <see cref="ProfissionalPublicoDto"/> (sem e-mail, sem datas, sem modelo
/// de permissão). Acessível a qualquer membro ativo do tenant, atende a
/// seletores em agenda/prontuário/orçamento sem vazar PII da equipe.
/// </summary>
public class ListarProfissionaisPublicoQuery : IQuery<IEnumerable<ProfissionalPublicoDto>>
{
    public long EstabelecimentoId { get; set; }
}
