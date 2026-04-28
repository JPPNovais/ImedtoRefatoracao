using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.ModelosPermissao.Commands;

public class AtualizarModeloPermissaoCommand : ICommand
{
    public long ModeloId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string TipoAcesso { get; set; } = "Profissional";
    public IReadOnlyList<string> Permissoes { get; set; } = Array.Empty<string>();
}
