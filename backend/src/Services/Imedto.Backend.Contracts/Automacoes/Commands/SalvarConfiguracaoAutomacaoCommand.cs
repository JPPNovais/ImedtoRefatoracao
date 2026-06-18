using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Automacoes.Commands;

public class SalvarConfiguracaoAutomacaoCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public bool LembretesHabilitados { get; set; }
    public bool LembretesWhatsappHabilitados { get; set; }
    public int HorasAntecedenciaLembrete { get; set; }
    public bool ExpiracaoOrcamentosHabilitada { get; set; }
    public string? EmailRemetente { get; set; }
}
