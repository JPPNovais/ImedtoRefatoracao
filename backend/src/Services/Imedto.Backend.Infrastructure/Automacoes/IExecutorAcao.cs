using System.Text.Json;

namespace Imedto.Backend.Infrastructure.Automacoes;

/// <summary>
/// Executa o array <c>acoes_json</c> de uma regra contra o payload do evento.
/// Falha de qualquer ação propaga via exception — o worker captura e aplica retry.
/// </summary>
public interface IExecutorAcao
{
    Task ExecutarAsync(string acoesJson, JsonDocument payload, long estabelecimentoId, CancellationToken ct);
}
