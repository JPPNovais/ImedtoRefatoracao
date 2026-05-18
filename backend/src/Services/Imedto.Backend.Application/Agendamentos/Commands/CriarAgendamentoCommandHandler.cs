using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Time;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class CriarAgendamentoCommandHandler : ICommandHandler<CriarAgendamentoCommand>
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly ISalaRepository _salaRepo;
    private readonly IEventBus _eventBus;

    public CriarAgendamentoCommandHandler(
        IAgendamentoRepository agendamentoRepo,
        IPacienteRepository pacienteRepo,
        IVinculoRepository vinculoRepo,
        IEstabelecimentoRepository estabelecimentoRepo,
        ISalaRepository salaRepo,
        IEventBus eventBus)
    {
        _agendamentoRepo = agendamentoRepo;
        _pacienteRepo = pacienteRepo;
        _vinculoRepo = vinculoRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
        _salaRepo = salaRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(CriarAgendamentoCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(cmd.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");
        if (paciente.DeletadoEm.HasValue)
            throw new BusinessException("Não é possível agendar para um paciente inativo.");

        var podeAtuar = await _vinculoRepo.PodeAtuarComoProfissional(
            cmd.ProfissionalUsuarioId, cmd.EstabelecimentoId);
        if (!podeAtuar)
            throw new BusinessException("Profissional não pode atuar neste estabelecimento.");

        await ValidarRegrasFuncionamento(cmd);

        if (cmd.SalaId.HasValue)
        {
            var sala = await _salaRepo.ObterPorIdOuNulo(cmd.SalaId.Value, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Sala não encontrada ou inativa.");
            if (!sala.Ativo)
                throw new BusinessException("Sala não encontrada ou inativa.");
        }

        var agendamento = Agendamento.Criar(
            cmd.EstabelecimentoId,
            cmd.PacienteId,
            cmd.ProfissionalUsuarioId,
            cmd.CriadoPorUsuarioId,
            cmd.InicioPrevisto,
            cmd.FimPrevisto,
            cmd.TipoServico,
            cmd.Observacoes);

        if (cmd.SalaId.HasValue)
            agendamento.AlocarSala(cmd.SalaId);

        await _agendamentoRepo.Salvar(agendamento);
        cmd.AgendamentoIdCriado = agendamento.Id;

        agendamento.MarcarComoCriado();
        foreach (var ev in agendamento.DomainEvents)
            await _eventBus.Publish(ev);
        agendamento.ClearDomainEvents();
    }

    private async Task ValidarRegrasFuncionamento(CriarAgendamentoCommand cmd)
    {
        var estab = await _estabelecimentoRepo.ObterPorId(cmd.EstabelecimentoId);

        // Converte UTC → Brasília para comparar com horário de funcionamento (configurado em BRT).
        estab.ValidarPodeAgendar(
            cmd.InicioPrevisto.ToBrasilia(),
            cmd.FimPrevisto.ToBrasilia(),
            BrasiliaTime.Now);

        // Conflito com agendamento existente do mesmo profissional NESTE estabelecimento
        // (profissional que atua em 2 estabs tem agendas independentes — defense-in-depth IDOR).
        var temConflito = await _agendamentoRepo.ExisteConflito(
            cmd.EstabelecimentoId,
            cmd.ProfissionalUsuarioId,
            cmd.InicioPrevisto,
            cmd.FimPrevisto);
        if (temConflito)
            throw new BusinessException("Já existe um agendamento nesse horário para este profissional.");
    }
}
