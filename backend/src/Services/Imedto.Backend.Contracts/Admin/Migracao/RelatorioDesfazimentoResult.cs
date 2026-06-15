namespace Imedto.Backend.Contracts.Admin.Migracao;

/// <summary>
/// Resultado do desfazer de migração (CA17, R9, D12).
/// Registros atualizados pelo upsert NÃO são revertidos — apenas reportados no aviso.
/// </summary>
public class RelatorioDesfazimentoResult
{
    /// <summary>Quantidade de registros que foram criados pelo job e revertidos com sucesso.</summary>
    public int TotalRevertidos { get; set; }

    /// <summary>
    /// Quantidade de registros criados pelo job que NÃO puderam ser revertidos
    /// (ex.: FK ativa de outro fluxo aponta para o registro).
    /// </summary>
    public int TotalNaoRevertidos { get; set; }

    /// <summary>
    /// Quantidade de registros que foram atualizados pelo upsert (já existiam antes do job)
    /// e por isso não fazem parte do rollback — aviso obrigatório de CA17.
    /// </summary>
    public int TotalAtualizadosMantidos { get; set; }

    /// <summary>Mensagem explicativa exibida ao operador.</summary>
    public string Aviso { get; set; } = string.Empty;
}
