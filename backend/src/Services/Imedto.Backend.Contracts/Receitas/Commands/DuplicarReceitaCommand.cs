using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Receitas.Commands;

/// <summary>
/// Duplica uma receita existente — cria nova com mesmos itens em status Emitida.
/// Não cria vínculo entre as duas (não é "nova versão" / Substituir).
/// </summary>
public class DuplicarReceitaCommand : ICommand
{
    public long ReceitaIdOrigem { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }

    /// <summary>Preenchido pelo handler — id da nova receita.</summary>
    public long ReceitaIdCriada { get; set; }
}
