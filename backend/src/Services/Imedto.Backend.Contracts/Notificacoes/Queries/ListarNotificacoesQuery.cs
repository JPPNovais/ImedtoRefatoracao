using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Notificacoes.Queries;

public class ListarNotificacoesQuery : IQuery<PaginaNotificacoesDto>
{
    public Guid UsuarioId { get; set; }

    /// <summary>null = todas, true = só lidas, false = só não-lidas (caso de uso típico do badge).</summary>
    public bool? Lidas { get; set; }

    public int Pagina { get; set; } = 1;
    public int Tamanho { get; set; } = 20;
}

public class ContadorNaoLidasQuery : IQuery<ContadorNaoLidasDto>
{
    public Guid UsuarioId { get; set; }
}

public class ContadorNaoLidasDto
{
    public int Total { get; set; }
}
