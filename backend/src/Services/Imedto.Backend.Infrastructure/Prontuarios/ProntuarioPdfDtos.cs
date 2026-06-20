namespace Imedto.Backend.Infrastructure.Prontuarios;

/// <summary>
/// DTOs internos do <see cref="QuestPdfProntuarioService"/> (leitura via Dapper).
/// InternalsVisibleTo("Imedto.Backend.Test") no Infrastructure.csproj expõe ao teste.
/// </summary>

internal sealed record ProntuarioCabecalhoRow(
    long ProntuarioId,
    long PacienteId,
    string PacienteNome,
    DateTime? PacienteDataNascimento,
    string PacienteGenero,
    string EstabelecimentoNomeFantasia,
    string EstabelecimentoCnpj,
    string EstabelecimentoTelefone,
    string EstabelecimentoEndereco,
    string EstabelecimentoFotoUrl,
    DateTime ProntuarioCriadoEm);

internal sealed record EvolucaoPdfRow(
    long Id,
    DateTime CriadaEm,
    string AutorNome,
    string ModeloNome,
    string ConteudoJson,
    string ModeloSnapshotJson,
    int ContagemAnexos);

internal sealed record DadosProntuarioPdf(
    ProntuarioCabecalhoRow Cabecalho,
    IReadOnlyList<EvolucaoPdfRow> Evolucoes)
{
    /// <summary>Bytes da logo do estabelecimento (null = usa placeholder de iniciais).</summary>
    public byte[] LogoBytes { get; init; }
}
