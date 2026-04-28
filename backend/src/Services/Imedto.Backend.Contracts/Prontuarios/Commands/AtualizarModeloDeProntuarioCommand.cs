using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

public class AtualizarModeloDeProntuarioCommand : ICommand
{
    public long ModeloId { get; set; }
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public string EstruturaJson { get; set; }
}
