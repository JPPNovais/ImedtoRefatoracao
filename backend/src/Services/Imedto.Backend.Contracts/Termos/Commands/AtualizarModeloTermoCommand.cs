using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Commands;

/// <summary>
/// Edita um modelo do estabelecimento. Só bumpa versão se o conteúdo HTML mudou.
/// Modelos padrão do sistema não são editáveis (clonar primeiro).
/// </summary>
public class AtualizarModeloTermoCommand : ICommand
{
    public long ModeloId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public string Categoria { get; set; }
    public string Titulo { get; set; }
    public string ConteudoHtml { get; set; }
}
