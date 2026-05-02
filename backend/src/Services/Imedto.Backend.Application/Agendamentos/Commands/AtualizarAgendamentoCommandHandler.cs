using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class AtualizarAgendamentoCommandHandler : ICommandHandler<AtualizarAgendamentoCommand>
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;

    public AtualizarAgendamentoCommandHandler(
        IAgendamentoRepository agendamentoRepo,
        IVinculoRepository vinculoRepo,
        IEstabelecimentoRepository estabelecimentoRepo)
    {
        _agendamentoRepo = agendamentoRepo;
        _vinculoRepo = vinculoRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
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

        var estab = await _estabelecimentoRepo.ObterPorId(cmd.EstabelecimentoId);
        estab.ValidarPodeAgendar(cmd.InicioPrevisto.ToLocalTime());

        if (await _agendamentoRepo.ExisteConflito(
                cmd.ProfissionalUsuarioId,
                cmd.InicioPrevisto,
                cmd.FimPrevisto,
                excluirAgendamentoId: cmd.AgendamentoId))
        {
            throw new BusinessException("Já existe um agendamento neste horário para este profissional.");
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
