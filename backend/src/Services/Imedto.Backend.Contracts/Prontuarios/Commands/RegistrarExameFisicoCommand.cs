using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Prontuarios.Commands;

/// <summary>
/// Registra um novo exame físico associado a uma evolução existente.
/// O exame inclui dados gerais (jsonb antropométrico/sinais vitais),
/// observações e a coleção inicial de regiões anatômicas com achados.
/// </summary>
public class RegistrarExameFisicoCommand : ICommand
{
    public long EvolucaoId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid AutorUsuarioId { get; set; }

    /// <summary>Pode vir nulo — handler usa <c>UtcNow</c>.</summary>
    public DateTime? RealizadoEm { get; set; }

    /// <summary>JSON livre — peso, altura, IMC, PA, FC, FR, sat, temperatura.</summary>
    public string? DadosGeraisJson { get; set; }

    public string? ObservacoesGerais { get; set; }

    public IEnumerable<RegiaoExameFisicoInput> Regioes { get; set; } = Array.Empty<RegiaoExameFisicoInput>();

    /// <summary>Preenchido pelo handler com o Id do exame criado (consumido pelo controller para retornar 201).</summary>
    public long ExameFisicoIdCriado { get; set; }
}

public class RegiaoExameFisicoInput
{
    public string Codigo { get; set; } = string.Empty;
    public string? PaiCodigo { get; set; }
    /// <summary>"Esquerda" | "Direita" | "Bilateral" | null/"NaoAplicavel".</summary>
    public string? Lateralidade { get; set; }
    public string? Achados { get; set; }
    /// <summary>"Normal" | "LeveAlteracao" | "Alterado" | "Critico".</summary>
    public string? Severidade { get; set; }
    public int Ordem { get; set; }
}
