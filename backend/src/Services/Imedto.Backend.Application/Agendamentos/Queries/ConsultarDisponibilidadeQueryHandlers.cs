using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Time;

namespace Imedto.Backend.Application.Agendamentos.Queries;

/// <summary>
/// Calcula a disponibilidade de um profissional para um intervalo de datas,
/// considerando: dias de funcionamento, horários bloqueados, datas bloqueadas
/// e agendamentos existentes ativos.
/// </summary>
public class ConsultarDisponibilidadeQueryHandlers : IRequestHandler<ConsultarDisponibilidadeQuery, DisponibilidadeSemanaDto>
{
    private static readonly string[] NomesDiaSemana = ["DOM", "SEG", "TER", "QUA", "QUI", "SEX", "SAB"];

    private readonly EstabelecimentoQueryRepository _estabRepo;
    private readonly AgendamentoQueryRepository _agendRepo;

    public ConsultarDisponibilidadeQueryHandlers(
        EstabelecimentoQueryRepository estabRepo,
        AgendamentoQueryRepository agendRepo)
    {
        _estabRepo = estabRepo;
        _agendRepo = agendRepo;
    }

    public async Task<DisponibilidadeSemanaDto> Handle(ConsultarDisponibilidadeQuery query)
    {
        var config = await _estabRepo.ObterConfiguracaoFuncionamento(query.EstabelecimentoId);

        // Defaults seguros se o estabelecimento não tiver configuração
        var horarioInicio = config?.HorarioInicio ?? new TimeOnly(8, 0);
        var horarioFim = config?.HorarioFim ?? new TimeOnly(18, 0);
        // Override do cliente prevalece — quando ausente, usa duracao padrao do estab.
        // Range valido protege contra valores absurdos (defense-in-depth: front ja limita,
        // mas backend eh fonte da verdade).
        var duracaoMin = query.DuracaoMinutos is int d && d >= 5 && d <= 480
            ? d
            : config?.DuracaoConsultaPadraoMinutos ?? 30;
        var intervaloMin = config?.IntervaloEntreConsultasMinutos ?? 0;
        var diasFunc = config?.DiasSemanaFuncionamento ?? new List<int> { 1, 2, 3, 4, 5 };
        var bloqueados = config?.HorariosBloqueados ?? new();
        var datasBloq = config?.DatasBloqueadas ?? new();

        var agendamentos = (await _agendRepo.ListarParaDisponibilidade(
            query.EstabelecimentoId,
            query.ProfissionalUsuarioId,
            query.DataInicio,
            query.DataFim)).ToList();

        // "Agora" em Brasília — fonte da verdade do tempo local. Independente do TZ do container.
        var agora = BrasiliaTime.Now;

        var slots = SlotGenerator.Gerar(horarioInicio, horarioFim, duracaoMin, intervaloMin);

        var resultado = new DisponibilidadeSemanaDto
        {
            ProfissionalUsuarioId = query.ProfissionalUsuarioId,
        };

        for (var data = query.DataInicio; data <= query.DataFim; data = data.AddDays(1))
        {
            var diaSemanaIdx = (int)data.DayOfWeek;
            var dia = new DisponibilidadeDiaDto
            {
                Data = data,
                DiaSemana = NomesDiaSemana[diaSemanaIdx],
            };

            bool diaFechado =
                !diasFunc.Contains(diaSemanaIdx) ||
                datasBloq.Any(db => db.Data == data);

            if (diaFechado)
            {
                dia.Status = "fechado";
                resultado.Dias.Add(dia);
                continue;
            }

            // Agendamentos ativos do profissional neste dia (compara em horário de Brasília).
            var agendsDia = agendamentos
                .Where(a => DateOnly.FromDateTime(a.InicioPrevisto.ToBrasilia()) == data)
                .ToList();

            foreach (var slotHora in slots)
            {
                var slotInicio = data.ToDateTime(slotHora);
                var slotFim = slotInicio.AddMinutes(duracaoMin);

                // Slot no passado? (data/horário já decorrido)
                if (slotInicio < agora)
                {
                    dia.Slots.Add(new DisponibilidadeSlotDto
                    {
                        Hora = $"{slotHora.Hour:D2}:{slotHora.Minute:D2}",
                        Disponivel = false,
                        Motivo = "passado",
                    });
                    continue;
                }

                // Horário bloqueado pelo estabelecimento? (overlap entre [slot, slot+duração] e o bloqueio)
                var slotHoraFim = slotHora.AddMinutes(duracaoMin);
                var bloqueio = bloqueados.FirstOrDefault(hb =>
                    slotHora < hb.Fim && slotHoraFim > hb.Inicio);

                if (bloqueio is not null)
                {
                    dia.Slots.Add(new DisponibilidadeSlotDto
                    {
                        Hora = $"{slotHora.Hour:D2}:{slotHora.Minute:D2}",
                        Disponivel = false,
                        Motivo = "bloqueado",
                    });
                    continue;
                }

                // Existe agendamento ativo que cobre este slot? (overlap em horário de Brasília)
                var agend = agendsDia.FirstOrDefault(a =>
                    a.InicioPrevisto.ToBrasilia() < slotFim &&
                    a.FimPrevisto.ToBrasilia() > slotInicio);

                if (agend is not null)
                {
                    dia.Slots.Add(new DisponibilidadeSlotDto
                    {
                        Hora = $"{slotHora.Hour:D2}:{slotHora.Minute:D2}",
                        Disponivel = false,
                        Motivo = "agendado",
                        PacienteNome = agend.PacienteNome,
                    });
                    continue;
                }

                dia.Slots.Add(new DisponibilidadeSlotDto
                {
                    Hora = $"{slotHora.Hour:D2}:{slotHora.Minute:D2}",
                    Disponivel = true,
                });
            }

            dia.Status = dia.Slots.Any(s => s.Disponivel) ? "disponivel" : "indisponivel";
            resultado.Dias.Add(dia);
        }

        return resultado;
    }

}
