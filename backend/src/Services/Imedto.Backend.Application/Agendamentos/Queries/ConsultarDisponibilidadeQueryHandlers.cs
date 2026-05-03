using Imedto.Backend.Contracts.Agendamentos.Queries;
using Imedto.Backend.Contracts.Agendamentos.Queries.Results;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;

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
        var duracaoMin = config?.DuracaoConsultaPadraoMinutos ?? 30;
        var intervaloMin = config?.IntervaloEntreConsultasMinutos ?? 0;
        var diasFunc = config?.DiasSemanaFuncionamento ?? new List<int> { 1, 2, 3, 4, 5 };
        var bloqueados = config?.HorariosBloqueados ?? new();
        var datasBloq = config?.DatasBloqueadas ?? new();

        var agendamentos = (await _agendRepo.ListarParaDisponibilidade(
            query.EstabelecimentoId,
            query.ProfissionalUsuarioId,
            query.DataInicio,
            query.DataFim)).ToList();

        // "Agora" em horário local — usado para marcar slots no passado.
        var agora = DateTime.Now;

        var slots = GerarSlots(horarioInicio, horarioFim, duracaoMin, intervaloMin);

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

            // Agendamentos ativos do profissional neste dia
            var agendsDia = agendamentos
                .Where(a => DateOnly.FromDateTime(a.InicioPrevisto.ToLocalTime()) == data)
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

                // Existe agendamento ativo que cobre este slot?
                var agend = agendsDia.FirstOrDefault(a =>
                    a.InicioPrevisto.ToLocalTime() < slotFim &&
                    a.FimPrevisto.ToLocalTime() > slotInicio);

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

    /// <summary>
    /// Gera slots em passos de (duração + intervalo) minutos a partir de <paramref name="inicio"/>,
    /// incluindo apenas slots cujo fim (inicio + duração) caiba até <paramref name="fim"/>.
    /// </summary>
    private static List<TimeOnly> GerarSlots(TimeOnly inicio, TimeOnly fim, int duracaoMin, int intervaloMin)
    {
        var lista = new List<TimeOnly>();
        var passo = duracaoMin + intervaloMin;
        if (passo <= 0) return lista;

        var atual = inicio;
        while (atual.AddMinutes(duracaoMin) <= fim)
        {
            lista.Add(atual);
            atual = atual.AddMinutes(passo);
        }
        return lista;
    }
}
