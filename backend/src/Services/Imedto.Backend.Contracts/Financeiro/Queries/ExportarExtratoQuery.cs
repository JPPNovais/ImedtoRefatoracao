using Imedto.Backend.Contracts.Financeiro.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Financeiro.Queries;

/// <summary>
/// Exporta todas as linhas do extrato do período + filtros (sem paginação).
/// Retorna <see cref="ExportarExtratoResultDto"/> com as linhas e metadados para audit (CA10).
/// Multi-tenant: filtra EstabelecimentoId via claim — falha-fechada.
/// LGPD: inclui PacienteNome (já exibido na tela com financeiro.ver); sem CPF/telefone (R9).
/// </summary>
public class ExportarExtratoQuery : IQuery<ExportarExtratoResultDto>
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }

    // Filtros idênticos à ListarExtratoQuery (R7 — export reflete mesmos filtros).
    public string? Tipo { get; set; }
    public string? Categoria { get; set; }
    public string? FormaPagamento { get; set; }
    public string? Origem { get; set; }
}
