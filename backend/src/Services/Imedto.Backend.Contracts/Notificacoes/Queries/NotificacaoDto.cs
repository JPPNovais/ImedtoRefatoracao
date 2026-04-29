namespace Imedto.Backend.Contracts.Notificacoes.Queries;

/// <summary>DTO de leitura — minimização LGPD: só campos consumidos pelo sino do frontend.</summary>
public class NotificacaoDto
{
    public long Id { get; set; }
    public long? EstabelecimentoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string? LinkAcao { get; set; }
    public bool Lida { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? LidaEm { get; set; }
}

public class PaginaNotificacoesDto
{
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamanho { get; set; }
    public IEnumerable<NotificacaoDto> Itens { get; set; } = Array.Empty<NotificacaoDto>();
}
