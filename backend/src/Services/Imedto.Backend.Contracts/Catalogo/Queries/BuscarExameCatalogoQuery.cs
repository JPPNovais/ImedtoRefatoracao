using Imedto.Backend.Contracts.Catalogo.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Catalogo.Queries;

public class BuscarExameCatalogoQuery : IQuery<IEnumerable<ExameCatalogoDto>>
{
    public string? Busca { get; set; }
    public int Limite { get; set; } = 30;
}
