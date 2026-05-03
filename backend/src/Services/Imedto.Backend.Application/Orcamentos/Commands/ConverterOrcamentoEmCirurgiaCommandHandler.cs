using Imedto.Backend.Contracts.Orcamentos.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Commands;

/// <summary>
/// Cria um <see cref="ProcedimentoCirurgico"/> a partir de um orçamento aprovado:
/// - Cirurgia principal: descrição da primeira <see cref="OrcamentoCirurgia"/>.
/// - Equipe: convertida de <see cref="OrcamentoEquipe"/> mapeando o papel string para
///   o enum <see cref="PapelCirurgia"/> (ignorando entradas sem mapeamento direto).
/// - Estabelecimento + paciente herdados; prontuário buscado pelo paciente.
/// - Após criar, vincula <c>Orcamento.ProcedimentoCirurgicoId</c> ao procedimento criado.
/// </summary>
public class ConverterOrcamentoEmCirurgiaCommandHandler : ICommandHandler<ConverterOrcamentoEmCirurgiaCommand>
{
    private readonly IOrcamentoRepository _orcRepo;
    private readonly IProcedimentoCirurgicoRepository _procRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IEventBus _events;

    public ConverterOrcamentoEmCirurgiaCommandHandler(
        IOrcamentoRepository orcRepo,
        IProcedimentoCirurgicoRepository procRepo,
        IProntuarioRepository prontuarioRepo,
        IEventBus events)
    {
        _orcRepo = orcRepo;
        _procRepo = procRepo;
        _prontuarioRepo = prontuarioRepo;
        _events = events;
    }

    public async Task Handle(ConverterOrcamentoEmCirurgiaCommand cmd)
    {
        var orc = await _orcRepo.ObterPorIdCompleto(cmd.OrcamentoId);
        if (orc.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Orçamento não encontrado.");
        if (orc.Status != OrcamentoStatus.Aprovado)
            throw new BusinessException("Apenas orçamentos aprovados podem ser convertidos em cirurgia.");
        if (orc.ProcedimentoCirurgicoId is not null && orc.ProcedimentoCirurgicoId != 0)
            throw new BusinessException("Este orçamento já foi convertido em cirurgia.");
        if (orc.Cirurgias.Count == 0)
            throw new BusinessException("Orçamento sem cirurgias não pode ser convertido.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(orc.PacienteId, orc.EstabelecimentoId);

        var cirurgiaPrincipal = orc.Cirurgias[0].Descricao ?? "Cirurgia";
        var equipeInicial = orc.Equipe
            .Select(e => (Papel: TentarMapearPapel(e.Papel), e.ProfissionalUsuarioId))
            .Where(t => t.Papel is not null)
            .Select(t => new ProcedimentoCirurgico.EquipeInicialPayload(t.ProfissionalUsuarioId, t.Papel!.Value))
            .GroupBy(p => new { p.ProfissionalUsuarioId, p.Papel })
            .Select(g => g.First())
            .ToList();

        var proc = ProcedimentoCirurgico.Planejar(
            pacienteId: orc.PacienteId,
            prontuarioId: prontuario.Id,
            estabelecimentoId: orc.EstabelecimentoId,
            agendamentoId: null,
            cirurgiaPrincipal: cirurgiaPrincipal,
            cirurgiaCodigo: null,
            dataAgendada: cmd.DataAgendada,
            equipeInicial: equipeInicial);

        await _procRepo.Salvar(proc);

        orc.RegistrarConversaoEmProcedimento(proc.Id);
        await _orcRepo.Salvar(orc);

        cmd.ProcedimentoCirurgicoIdCriado = proc.Id;

        foreach (var ev in proc.DomainEvents)
            await _events.Publish(ev);
        proc.ClearDomainEvents();
    }

    /// <summary>Mapa orçamento (string livre) → enum operacional. Ignora não-mapeados.</summary>
    private static PapelCirurgia? TentarMapearPapel(string papel) => papel.ToLowerInvariant() switch
    {
        "cirurgião" or "cirurgiao" => PapelCirurgia.Cirurgiao,
        "auxiliar" => PapelCirurgia.Auxiliar,
        "anestesista" or "anestesiologista" => PapelCirurgia.Anestesista,
        "instrumentador" => PapelCirurgia.Instrumentador,
        "circulante" => PapelCirurgia.Circulante,
        _ => null,
    };
}
