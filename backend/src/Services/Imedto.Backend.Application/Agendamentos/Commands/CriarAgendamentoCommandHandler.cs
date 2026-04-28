using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class CriarAgendamentoCommandHandler : ICommandHandler<CriarAgendamentoCommand>
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IVinculoRepository _vinculoRepo;
    private readonly IEstabelecimentoRepository _estabelecimentoRepo;
    private readonly IEventBus _eventBus;

    public CriarAgendamentoCommandHandler(
        IAgendamentoRepository agendamentoRepo,
        IPacienteRepository pacienteRepo,
        IVinculoRepository vinculoRepo,
        IEstabelecimentoRepository estabelecimentoRepo,
        IEventBus eventBus)
    {
        _agendamentoRepo = agendamentoRepo;
        _pacienteRepo = pacienteRepo;
        _vinculoRepo = vinculoRepo;
        _estabelecimentoRepo = estabelecimentoRepo;
        _eventBus = eventBus;
    }

    public async Task Handle(CriarAgendamentoCommand cmd)
    {
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(cmd.PacienteId);
        if (paciente is null || paciente.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Paciente não encontrado neste estabelecimento.");
        if (paciente.DeletadoEm.HasValue)
            throw new BusinessException("Não é possível agendar para um paciente inativo.");

        var podeAtuar = await _vinculoRepo.PodeAtuarComoProfissional(
            cmd.ProfissionalUsuarioId, cmd.EstabelecimentoId);
        if (!podeAtuar)
            throw new BusinessException("Profissional não pode atuar neste estabelecimento.");

        await ValidarRegrasFuncionamento(cmd);

        var agendamento = Agendamento.Criar(
            cmd.EstabelecimentoId,
            cmd.PacienteId,
            cmd.ProfissionalUsuarioId,
            cmd.CriadoPorUsuarioId,
            cmd.InicioPrevisto,
            cmd.FimPrevisto,
            cmd.TipoServico,
            cmd.Observacoes);

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

        // Converte UTC → local para comparar com horário de funcionamento (sem timezone no banco)
        var inicioLocal = cmd.InicioPrevisto.ToLocalTime();
        var data        = DateOnly.FromDateTime(inicioLocal);
        var horaInicio  = TimeOnly.FromDateTime(inicioLocal);

        // Dia da semana (0=Dom, 1=Seg, …, 6=Sab)
        var diaSemana = (int)inicioLocal.DayOfWeek;
        if (!estab.DiasSemanaFuncionamento.Contains(diaSemana))
            throw new BusinessException("O estabelecimento não funciona neste dia da semana.");

        // Data bloqueada
        if (estab.DatasBloqueadas.Any(db => db.Data == data))
            throw new BusinessException("Esta data está bloqueada no estabelecimento.");

        // Horário de funcionamento
        if (horaInicio < estab.HorarioInicio || horaInicio >= estab.HorarioFim)
            throw new BusinessException(
                $"O agendamento deve estar dentro do horário de funcionamento " +
                $"({estab.HorarioInicio:HH\\:mm}–{estab.HorarioFim:HH\\:mm}).");

        // Horário bloqueado
        var bloqueio = estab.HorariosBloqueados.FirstOrDefault(hb =>
            horaInicio >= hb.Inicio && horaInicio < hb.Fim);
        if (bloqueio is not null)
        {
            var desc = string.IsNullOrWhiteSpace(bloqueio.Descricao) ? "" : $" ({bloqueio.Descricao})";
            throw new BusinessException($"Este horário está bloqueado{desc}: {bloqueio.Inicio:HH\\:mm}–{bloqueio.Fim:HH\\:mm}.");
        }

        // Conflito com agendamento existente do mesmo profissional
        var temConflito = await _agendamentoRepo.ExisteConflito(
            cmd.ProfissionalUsuarioId,
            cmd.InicioPrevisto,
            cmd.FimPrevisto);
        if (temConflito)
            throw new BusinessException("Já existe um agendamento nesse horário para este profissional.");
    }
}
