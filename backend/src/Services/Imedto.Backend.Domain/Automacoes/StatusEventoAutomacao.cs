namespace Imedto.Backend.Domain.Automacoes;

/// <summary>
/// Estados de um <see cref="EventoAutomacao"/>. Persistido como string no Postgres
/// (legibilidade direta na tabela, mesmo padrão do <c>JobStatus</c>).
/// </summary>
public enum StatusEventoAutomacao
{
    /// <summary>Pronto para ser pego pelo worker quando <c>executar_em &lt;= now()</c>.</summary>
    Pendente,

    /// <summary>Reservado pelo worker e em execução.</summary>
    Executando,

    /// <summary>Executado com sucesso (terminal).</summary>
    Concluido,

    /// <summary>Esgotou tentativas (terminal — não tenta mais automaticamente).</summary>
    Falhou
}
