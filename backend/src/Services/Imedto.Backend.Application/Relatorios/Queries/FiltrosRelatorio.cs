using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Relatorios.Queries;

/// <summary>
/// Validações compartilhadas de filtros de relatório. Garante intervalo coerente e
/// limita a 1 ano para proteger o banco de queries acidentalmente abusivas.
/// </summary>
internal static class FiltrosRelatorio
{
    public static void Validar(DateOnly dataInicio, DateOnly dataFim)
    {
        if (dataInicio > dataFim)
            throw new BusinessException("Data inicial não pode ser maior que a final.");

        // 366 dias para tolerar ano bissexto (relatório anual).
        var dias = dataFim.DayNumber - dataInicio.DayNumber;
        if (dias > 366)
            throw new BusinessException("Intervalo máximo permitido é de 1 ano.");
    }
}
