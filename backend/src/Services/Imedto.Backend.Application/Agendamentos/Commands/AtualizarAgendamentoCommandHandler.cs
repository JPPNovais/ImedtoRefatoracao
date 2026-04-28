using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class AtualizarAgendamentoCommandHandler : ICommandHandler<AtualizarAgendamentoCommand>
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IVinculoRepository _vinculoRepo;

    public AtualizarAgendamentoCommandHandler(
        IAgendamentoRepository agendamentoRepo,
        IVinculoRepository vinculoRepo)
    {
        _agendamentoRepo = agendamentoRepo;
        _vinculoRepo = vinculoRepo;
    }

    public async Task Handle(AtualizarAgendamentoCommand cmd)
    {
        var agendamento = await _agendamentoRepo.ObterPorId(cmd.AgendamentoId);

        if (agendamento.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Agendamento não encontrado neste estabelecimento.");

        if (cmd.ProfissionalUsuarioId != agendamento.ProfissionalUsuarioId)
        {
            var podeAtuar = await _vinculoRepo.PodeAtuarComoProfissional(
                cmd.ProfissionalUsuarioId, cmd.EstabelecimentoId);
            if (!podeAtuar)
                throw new BusinessException("Profissional não pode atuar neste estabelecimento.");
        }

        agendamento.Atualizar(
            cmd.ProfissionalUsuarioId,
            cmd.InicioPrevisto,
            cmd.FimPrevisto,
            cmd.TipoServico,
            cmd.Observacoes);

        await _agendamentoRepo.Salvar(agendamento);
    }
}
