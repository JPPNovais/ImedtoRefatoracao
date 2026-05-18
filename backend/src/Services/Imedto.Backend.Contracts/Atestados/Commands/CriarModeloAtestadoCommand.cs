using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Atestados.Commands;

public class CriarModeloAtestadoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Comparecimento";
    public string Conteudo { get; set; } = string.Empty;

    public long ModeloIdCriado { get; set; }
}

public class AtualizarModeloAtestadoCommand : ICommand
{
    public long ModeloId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Comparecimento";
    public string Conteudo { get; set; } = string.Empty;
}

public class ExcluirModeloAtestadoCommand : ICommand
{
    public long ModeloId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid SolicitanteUsuarioId { get; set; }
}
