namespace Imedto.Backend.Infrastructure.Receitas;

/// <summary>
/// DTOs internos do <see cref="QuestPdfReceitaService"/> (leitura via Dapper).
/// Extraídos para arquivo próprio para que <c>CarregarDadosAsync</c> possa ser
/// <c>protected internal virtual</c> sem gerar CS0050 (tipo de retorno menos
/// acessível que o método).
/// InternalsVisibleTo("Imedto.Backend.Test") configurado no Infrastructure.csproj
/// expõe esses tipos aos testes de unidade.
/// </summary>

internal sealed record ReceitaRow(
    long Id,
    string Tipo,
    string TipoNotificacao,
    string Status,
    string AssinaturaDigitalStatus,
    DateTime? EmitidaEm,
    DateTime? ValidadeAte,
    string Observacoes,
    string MotivoCancelamento,
    long PacienteId,
    string PacienteNome,
    string PacienteCpf,
    DateTime? PacienteDataNascimento,
    string PacienteGenero,
    string PacienteTelefone,
    string ProfissionalNome,
    string ProfissionalCrmCro,
    string CabecalhoHtml,
    string RodapeHtml,
    string EmissorPadrao,
    string EstabelecimentoNomeFantasia,
    string EstabelecimentoCnpj,
    string EstabelecimentoTelefone,
    string EstabelecimentoEndereco,
    string EstabelecimentoFotoUrl,
    // Gating autor-ou-dono (briefing 2026-06-27_001): usado em GerarAsync para verificar acesso ao PDF.
    Guid ProfissionalUsuarioId);

internal sealed record ItemRow(
    int Ordem,
    string Medicamento,
    string Posologia,
    string Concentracao,
    string FormaFarmaceutica,
    string Via,
    string Quantidade,
    string Duracao,
    string Observacao);

internal sealed record DadosPdf(ReceitaRow Receita, List<ItemRow> Itens)
{
    /// <summary>Bytes da logo do estabelecimento (null = usa placeholder de iniciais).</summary>
    public byte[] LogoBytes { get; init; }
}
