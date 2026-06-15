using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Migracao.Commands;

/// <summary>
/// Inicia uma migração: cria o job, valida o arquivo (≤ 50MB — CA19, R11),
/// salva o ZIP no S3 (retenção 30 dias — CA24, R12), e registra o arquivo no job.
/// </summary>
public sealed class IniciarMigracaoCommand : ICommand
{
    public required long EstabelecimentoId { get; init; }
    public required Guid UsuarioId { get; init; }

    /// <summary>Conteúdo do ZIP. Stream consumida pelo handler — não fechar antes.</summary>
    public required Stream ArquivoStream { get; init; }

    /// <summary>Tamanho em bytes do arquivo, para validação de limite (CA19).</summary>
    public required long ArquivoTamanhoBytes { get; init; }

    /// <summary>Nome do arquivo informado pelo cliente (apenas para log — sem PII).</summary>
    public string ArquivoNomeOriginal { get; init; } = string.Empty;

    /// <summary>Sistema de origem informado pelo cliente (opcional).</summary>
    public string? Origem { get; init; }

    /// <summary>
    /// Onda de carga. Null = Onda 1 (padrão — pacientes, estoque, agenda, orçamento).
    /// "prontuario" = Onda 2 (bloqueada até Onda 1 concluir — CA13).
    /// </summary>
    public string? Onda { get; init; }
}

/// <summary>Resultado do command — Id do job criado.</summary>
public sealed class IniciarMigracaoResult
{
    public long JobId { get; init; }
    public string Status { get; init; } = string.Empty;
}
