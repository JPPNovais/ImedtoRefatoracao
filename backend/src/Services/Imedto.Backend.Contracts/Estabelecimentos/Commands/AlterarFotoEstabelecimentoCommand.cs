using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Estabelecimentos.Commands;

/// <summary>
/// Sobe a foto/logo do estabelecimento para o storage e atualiza a URL no aggregate.
/// Apenas o dono do estabelecimento pode alterar.
/// </summary>
public class AlterarFotoEstabelecimentoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid UsuarioSolicitanteId { get; set; }
    public string MimeType { get; set; }
    public string Extensao { get; set; }
    public Stream Conteudo { get; set; }
}
