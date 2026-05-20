using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Commands;

/// <summary>
/// Cria um modelo de termo no estabelecimento atual. O handler sanitiza
/// <see cref="ConteudoHtml"/> antes de persistir e cria também a versão 1 imutável.
/// </summary>
public class CriarModeloTermoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public string Categoria { get; set; }
    public string Titulo { get; set; }
    public string ConteudoHtml { get; set; }

    /// <summary>Preenchido pelo handler — id do modelo criado.</summary>
    public long ModeloIdCriado { get; set; }
}
