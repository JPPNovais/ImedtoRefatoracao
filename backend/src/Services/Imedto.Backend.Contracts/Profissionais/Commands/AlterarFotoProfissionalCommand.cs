using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Profissionais.Commands;

/// <summary>
/// Sobe a foto do profissional autenticado para o storage e atualiza a URL no aggregate.
/// O <c>Conteudo</c> é consumido pelo handler e fechado pelo caller (controller).
/// </summary>
public class AlterarFotoProfissionalCommand : ICommand
{
    public Guid UsuarioId { get; set; }
    public string MimeType { get; set; }
    public string Extensao { get; set; }
    public Stream Conteudo { get; set; }
}
