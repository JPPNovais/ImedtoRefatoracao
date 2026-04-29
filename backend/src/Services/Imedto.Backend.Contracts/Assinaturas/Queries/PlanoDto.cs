namespace Imedto.Backend.Contracts.Assinaturas.Queries;

/// <summary>
/// DTO de leitura do plano. Inclui apenas os campos consumidos pela tela de "minha assinatura"
/// e pelo seletor de planos (sem auditoria/timestamps internos).
/// </summary>
public class PlanoDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal PrecoMensal { get; set; }

    /// <summary>null = ilimitado.</summary>
    public int? LimiteProfissionais { get; set; }

    /// <summary>null = ilimitado.</summary>
    public int? LimitePacientes { get; set; }

    /// <summary>Lista de chaves de feature (ver <c>Domain.Assinaturas.Features</c>).</summary>
    public IEnumerable<string> Features { get; set; } = Array.Empty<string>();

    public bool Ativo { get; set; }
    public int Ordem { get; set; }
}
