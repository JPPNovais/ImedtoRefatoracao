using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Queries;

/// <summary>
/// Lista paginada de modelos de termo. Sempre inclui modelos do tenant; pode opcionalmente
/// incluir padrões do sistema (<see cref="IncluirPadroes"/>).
/// </summary>
public class ListarModelosTermoQuery : IQuery<PaginaModelosTermoDto>
{
    public long EstabelecimentoId { get; set; }
    public bool SomenteAtivos { get; set; }
    /// <summary>Filtro opcional por categoria (lgpd/cirurgico/imagem/financeiro/telemedicina/geral).</summary>
    public string Categoria { get; set; }
    public bool IncluirPadroes { get; set; } = true;
    public int Pagina { get; set; } = 1;
    public int Tamanho { get; set; } = 10;
    public string Busca { get; set; }
}
