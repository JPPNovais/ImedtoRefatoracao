using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Pacientes.Queries;

/// <summary>
/// Query agregada de documentos clínicos finalizados de um paciente
/// (receitas emitidas, atestados e pedidos de exame) com paginação server-side.
/// Suporta filtro por tipo, período e busca textual por subconsulta antes do UNION.
/// </summary>
public class ListarDocumentosDoPacienteQuery : IQuery<PaginaDocumentosDto>
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;

    /// <summary>Filtro por tipo: "Receita" | "Atestado" | "PedidoExame". Ausente = todos.</summary>
    public string? Tipo { get; set; }

    /// <summary>Início do período (inclusivo), UTC. Ausente = sem limite inferior.</summary>
    public DateTime? DataInicio { get; set; }

    /// <summary>Fim do período (inclusivo), UTC. Ausente = sem limite superior.</summary>
    public DateTime? DataFim { get; set; }

    /// <summary>
    /// Termo de busca textual livre (R8/R11). Aplicado por subconsulta antes do UNION:
    /// receita → medicamentos; atestado → tipo + conteúdo; pedido → exames + indicação.
    /// Vazio/nulo = sem filtro textual.
    /// </summary>
    public string? Busca { get; set; }
}
