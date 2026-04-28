using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Automacoes.Queries;

public class ObterConfiguracaoAutomacaoQuery : IQuery<ConfiguracaoAutomacaoDto>
{
    public long EstabelecimentoId { get; set; }
}

public class ConfiguracaoAutomacaoDto
{
    public bool LembretesHabilitados { get; set; }
    public int HorasAntecedenciaLembrete { get; set; }
    public bool ExpiracaoOrcamentosHabilitada { get; set; }
    public string? EmailRemetente { get; set; }
}
