using System.Threading;
using System.Threading.Tasks;

namespace Imedto.Backend.EtlValidator.Validacoes;

public interface IValidacao
{
    string Nome { get; }
    Task ExecutarAsync(RelatorioCompleto relatorio, CancellationToken ct);
}
