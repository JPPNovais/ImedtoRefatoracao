using Imedto.Backend.Domain.Inventario.Events;
using Imedto.Backend.Domain.Notificacoes;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Microsoft.Extensions.Logging;

// Nota: IInventarioNotificacaoQueryRepository fica em Infrastructure para manter
// a query Dapper junto com a implementação. O handler de Application referencia a
// interface — inversão de dependência sem criar projeto de Contracts para um único método.

namespace Imedto.Backend.Application.Inventario.Events;

/// <summary>
/// Reage ao <see cref="EstoqueAbaixoMinimoEvent"/> e cria uma notificação interna
/// para cada usuário com a ação <c>estoque</c> no estabelecimento do item.
///
/// O evento já é disparado pelo domínio <b>apenas no cruzamento descendente</b>
/// (quantidade anterior >= mínimo → quantidade atual &lt; mínimo), portanto este handler
/// não precisa se preocupar com anti-spam — o domínio garante a semântica.
///
/// A movimentação que originou o evento já foi persistida e commitada antes do Publish;
/// falhas aqui não devem reverter o estoque. Por isso, o handler captura exceções por
/// destinatário — um erro isolado não impede os demais.
///
/// LGPD: a mensagem contém apenas nome do item, quantidades e unidade (R5).
/// Multi-tenant: destinatários resolvidos exclusivamente do mesmo estabelecimento (R4).
/// </summary>
public class EstoqueAbaixoMinimoEventHandler : IEventHandler<EstoqueAbaixoMinimoEvent>
{
    private readonly INotificacaoService _notificacoes;
    private readonly IInventarioNotificacaoQueryRepository _destinatariosQuery;
    private readonly ILogger<EstoqueAbaixoMinimoEventHandler> _logger;

    public EstoqueAbaixoMinimoEventHandler(
        INotificacaoService notificacoes,
        IInventarioNotificacaoQueryRepository destinatariosQuery,
        ILogger<EstoqueAbaixoMinimoEventHandler> logger)
    {
        _notificacoes = notificacoes;
        _destinatariosQuery = destinatariosQuery;
        _logger = logger;
    }

    public async Task Handle(EstoqueAbaixoMinimoEvent domainEvent)
    {
        var destinatarios = await _destinatariosQuery
            .ListarUsuariosComAcaoEstoque(domainEvent.EstabelecimentoId);

        if (destinatarios.Count == 0)
        {
            // Estabelecimento sem usuários com ação estoque — situação válida mas incomum.
            // Log de info sem PII (não loga nome do item nem quantidades).
            _logger.LogInformation(
                "Cruzamento de estoque mínimo: nenhum destinatário com ação estoque no estabelecimento {EstabelecimentoId}. ItemId={ItemId}",
                domainEvent.EstabelecimentoId,
                domainEvent.ItemInventarioId);
            return;
        }

        // Mensagem montada uma vez — idêntica para todos os destinatários (R5: sem PII).
        var titulo = "Estoque abaixo do mínimo";
        var mensagem = $"O item \"{domainEvent.ItemNome}\" atingiu {domainEvent.QuantidadeAtual:G29} " +
                       $"(mínimo: {domainEvent.QuantidadeMinima:G29}). Considere repor.";
        var linkAcao = "/inventario?aba=alertas";

        foreach (var usuarioId in destinatarios)
        {
            try
            {
                await _notificacoes.EnviarAsync(
                    usuarioId: usuarioId,
                    estabelecimentoId: domainEvent.EstabelecimentoId,
                    titulo: titulo,
                    mensagem: mensagem,
                    categoria: CategoriaNotificacao.Estoque,
                    linkAcao: linkAcao);
            }
            catch (Exception ex)
            {
                // Falha isolada por destinatário não deve impedir os demais nem reverter a movimentação.
                // Log técnico sem PII — só IDs estruturados.
                _logger.LogError(ex,
                    "Falha ao criar notificação de estoque mínimo. UsuarioId={UsuarioId} EstabelecimentoId={EstabelecimentoId} ItemId={ItemId}",
                    usuarioId,
                    domainEvent.EstabelecimentoId,
                    domainEvent.ItemInventarioId);
            }
        }
    }
}
