using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Events;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Application.Orcamentos.Events;

/// <summary>
/// F5 — Gera (ou sincroniza) a Cobrança de cirurgia na aprovação do orçamento (R5/R6/R8).
///
/// Fluxo:
///  1. Consulta cobrança existente pelo orcamento_id (idempotência de domínio R6).
///  2. Se não existe: cria Cobranca.CriarParaCirurgia e persiste (CA101).
///  3. Se existe com mesmo valor: no-op (CA103).
///  4. Se existe com valor diferente: SincronizarValorCobrado — grava histórico (CA105).
///     Bloqueio de redução abaixo do pago líquido → BusinessException 422 (R9/CA107).
///
/// Atomicidade: executado dentro do EfUnitOfWorkScope da aprovação (briefing §1).
/// Se este handler lançar, toda a aprovação reverte (CA102).
/// </summary>
public class OrcamentoAprovadoEventHandler : IEventHandler<OrcamentoAprovadoEvent>
{
    private readonly ICobrancaRepository _cobrancaRepo;
    private readonly IOrcamentoRepository _orcamentoRepo;

    public OrcamentoAprovadoEventHandler(
        ICobrancaRepository cobrancaRepo,
        IOrcamentoRepository orcamentoRepo)
    {
        _cobrancaRepo = cobrancaRepo;
        _orcamentoRepo = orcamentoRepo;
    }

    public async Task Handle(OrcamentoAprovadoEvent domainEvent)
    {
        var orcamento = await _orcamentoRepo.ObterPorIdCompletoOuNulo(domainEvent.OrcamentoId, domainEvent.EstabelecimentoId)
            ?? throw new BusinessException("Orçamento não encontrado.");

        // Guard F5: apenas orçamentos com cirurgias geram cobrança (CA116).
        // Orçamentos de itens simples (sem cirurgias) não produzem cobrança aqui.
        if (!orcamento.Cirurgias.Any())
            return;

        // Descrição sem PII (R12/CA113) — usa número do orçamento se disponível.
        var descricao = string.IsNullOrWhiteSpace(orcamento.Numero)
            ? $"Cirurgia — orçamento #{orcamento.Id}"
            : $"Cirurgia — orçamento {orcamento.Numero}";

        var cobrancaExistente = await _cobrancaRepo.ObterPorOrcamentoOuNulo(
            domainEvent.OrcamentoId, domainEvent.EstabelecimentoId);

        if (cobrancaExistente is null)
        {
            // Caminho feliz: primeira aprovação → cria cobrança (CA101).
            var cobranca = Cobranca.CriarParaCirurgia(
                estabelecimentoId: domainEvent.EstabelecimentoId,
                pacienteId: domainEvent.PacienteId,
                orcamentoId: domainEvent.OrcamentoId,
                valorCobrado: domainEvent.Total,
                descricao: descricao,
                criadoPorUsuarioId: orcamento.CriadoPorUsuarioId);

            await _cobrancaRepo.Salvar(cobranca);
            return;
        }

        // Cobrança já existe: sincroniza valor (R8). No-op se total idêntico (CA103).
        // SincronizarValorCobrado lança BusinessException se redução abaixo do pago (R9/CA107).
        var contagemHistoricoAntes = cobrancaExistente.HistoricoValor.Count;
        cobrancaExistente.SincronizarValorCobrado(domainEvent.Total, orcamento.CriadoPorUsuarioId);

        // Salva apenas se foi gerado novo histórico (valor era diferente — não é no-op).
        if (cobrancaExistente.HistoricoValor.Count > contagemHistoricoAntes)
            await _cobrancaRepo.Salvar(cobrancaExistente);
    }
}
