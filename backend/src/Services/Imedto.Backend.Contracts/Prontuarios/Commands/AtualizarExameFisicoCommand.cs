using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

/// <summary>
/// Atualiza um exame físico existente: dados gerais + observações + sincronização
/// completa da coleção de regiões. Regiões fora da lista enviada são removidas;
/// regiões novas são adicionadas; existentes têm achados/severidade/lateralidade atualizados.
/// </summary>
public class AtualizarExameFisicoCommand : ICommand
{
    public long ExameFisicoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid AutorUsuarioId { get; set; }

    public string? DadosGeraisJson { get; set; }
    public string? ObservacoesGerais { get; set; }

    public IEnumerable<RegiaoExameFisicoInput> Regioes { get; set; } = Array.Empty<RegiaoExameFisicoInput>();
}
