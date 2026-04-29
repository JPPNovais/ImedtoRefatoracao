using Imedto.Backend.Domain.Assinaturas;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Vinculos.Events;

/// <summary>
/// Reage à aprovação de uma solicitação inversa criando o
/// <see cref="VinculoProfissionalEstabelecimento"/> automaticamente, com status
/// <see cref="VinculoStatus.Ativo"/> e o modelo de permissão padrão do estabelecimento.
///
/// Decisão (sequência <c>Convidar</c> + <c>Aceitar</c> automática):
/// - O profissional já manifestou interesse ao solicitar; o dono já consentiu ao aprovar.
/// - Em vez de adicionar um novo construtor de aggregate só para este caso, reusamos
///   a fábrica <c>Convidar</c> seguida de <c>Aceitar</c> dentro da MESMA transação. Resultado
///   visível: vínculo já <see cref="VinculoStatus.Ativo"/> — o profissional não precisa
///   "aceitar" novamente na tela de convites.
/// - Os eventos de domínio (<see cref="ProfissionalConvidadoEvent"/> e
///   <see cref="VinculoAceitoEvent"/>) são disparados normalmente, mantendo audit trail
///   consistente com o fluxo "clínica convida → profissional aceita".
/// - Re-uso do gating de plano: <see cref="IAssinaturaService.LimiteAtingidoAsync"/>.
///
/// Idempotência:
/// - Se já houver vínculo Ativo/Convidado (corrida com fluxo direto de convite), no-op.
/// - Se houver vínculo Inativo (re-vínculo), reativa via
///   <c>ReativarComoConvite</c> + <c>Aceitar</c> — preserva linha histórica.
///
/// Roda na MESMA transação do command (UnitOfWorkFilter) — falha aqui ⇒ rollback de tudo.
/// </summary>
public class AoAprovarSolicitacaoCriarVinculoHandler : IEventHandler<SolicitacaoVinculoAprovadaEvent>
{
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IModeloPermissaoRepository _modeloRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IAssinaturaService _assinaturaService;
    private readonly IEventBus _eventBus;

    public AoAprovarSolicitacaoCriarVinculoHandler(
        IVinculoRepository vinculoRepo,
        IModeloPermissaoRepository modeloRepo,
        IUsuarioRepository usuarioRepo,
        IAssinaturaService assinaturaService,
        IEventBus eventBus)
    {
        _vinculoRepo = vinculoRepo;
        _modeloRepo = modeloRepo;
        _usuarioRepo = usuarioRepo;
        _assinaturaService = assinaturaService;
        _eventBus = eventBus;
    }

    public async Task Handle(SolicitacaoVinculoAprovadaEvent @event)
    {
        if (await _assinaturaService.LimiteAtingidoAsync(@event.EstabelecimentoId, "profissionais"))
            throw new BusinessException("Plano não permite mais profissionais. Faça upgrade.");

        // Conta local do profissional precisa existir antes da aprovação. Em prática,
        // chegou até aqui = passou pelo onboarding (caso contrário não teria sub no JWT
        // para criar a solicitação). Esta checagem é defesa em profundidade.
        var existente = await _usuarioRepo.ObterPorIdOuNulo(@event.ProfissionalUsuarioId);
        if (existente is null)
            throw new BusinessException("Profissional precisa ter conta ativa antes da aprovação.");

        var modeloPadrao = await _modeloRepo.ObterPadraoDoEstabelecimento(@event.EstabelecimentoId);
        if (modeloPadrao is null)
            throw new BusinessException("Estabelecimento não possui modelo de permissão padrão.");

        var vinculoExistente = await _vinculoRepo.ObterPorProfissionalEEstabelecimentoOuNulo(
            @event.ProfissionalUsuarioId, @event.EstabelecimentoId);

        VinculoProfissionalEstabelecimento vinculo;

        if (vinculoExistente is { Status: VinculoStatus.Ativo or VinculoStatus.Convidado })
        {
            // Já há vínculo válido (corrida ou inconsistência). No-op silencioso.
            return;
        }

        if (vinculoExistente is { Status: VinculoStatus.Inativo })
        {
            // Reativa preservando linha histórica.
            vinculoExistente.ReativarComoConvite(modeloPadrao.Id, @event.AprovadoPorUsuarioId);
            vinculoExistente.Aceitar();
            vinculo = vinculoExistente;
            await _vinculoRepo.Salvar(vinculo);
        }
        else
        {
            // Novo vínculo: Convidar + Aceitar em sequência (transação garante atomicidade).
            vinculo = VinculoProfissionalEstabelecimento.Convidar(
                @event.ProfissionalUsuarioId,
                @event.EstabelecimentoId,
                modeloPadrao.Id,
                @event.AprovadoPorUsuarioId);

            await _vinculoRepo.Salvar(vinculo);  // popula Id
            vinculo.MarcarComoConvidado();        // anexa ProfissionalConvidadoEvent
            vinculo.Aceitar();                    // muda para Ativo + anexa VinculoAceitoEvent
            await _vinculoRepo.Salvar(vinculo);
        }

        foreach (var evt in vinculo.DomainEvents)
            await _eventBus.Publish(evt);

        vinculo.ClearDomainEvents();
    }
}
