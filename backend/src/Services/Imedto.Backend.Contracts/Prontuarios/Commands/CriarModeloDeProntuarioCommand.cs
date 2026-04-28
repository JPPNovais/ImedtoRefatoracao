using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

public class CriarModeloDeProntuarioCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public string EstruturaJson { get; set; }
}
