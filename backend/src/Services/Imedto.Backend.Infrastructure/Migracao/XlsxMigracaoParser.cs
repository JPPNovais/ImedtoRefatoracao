using Imedto.Backend.Domain.Migracao;

namespace Imedto.Backend.Infrastructure.Migracao;

/// <summary>
/// Stub para XLSX — formato não suportado nesta versão.
/// Converta o arquivo para CSV antes de importar.
/// </summary>
public sealed class XlsxMigracaoParser : IMigracaoArquivoParser
{
    public bool SuportaFormato(string extensao) =>
        extensao.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
        extensao.Equals(".xls", StringComparison.OrdinalIgnoreCase);

    public Task<ArquivoParseado> ParsearAsync(
        Stream stream,
        string nomeArquivo,
        CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "Formato XLSX não suportado nesta versão. Converta para CSV antes de importar.");
    }
}
