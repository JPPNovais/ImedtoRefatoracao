namespace Imedto.Backend.Application.Agendamentos.Queries;

/// <summary>
/// Gera o grid de slots de horario respeitando duracao do agendamento +
/// intervalo entre consultas. Funcao pura (sem I/O) — testavel isoladamente.
///
/// O passo entre dois inicios consecutivos eh <c>duracao + intervalo</c>.
/// Um slot so eh emitido se <c>inicio + duracao &lt;= horarioFim</c>.
/// </summary>
public static class SlotGenerator
{
    public static List<TimeOnly> Gerar(TimeOnly inicio, TimeOnly fim, int duracaoMin, int intervaloMin)
    {
        var lista = new List<TimeOnly>();
        var passo = duracaoMin + intervaloMin;
        if (passo <= 0 || duracaoMin <= 0) return lista;
        if (fim <= inicio) return lista;

        var atual = inicio;
        while (atual.AddMinutes(duracaoMin) <= fim)
        {
            lista.Add(atual);
            atual = atual.AddMinutes(passo);
        }
        return lista;
    }
}
