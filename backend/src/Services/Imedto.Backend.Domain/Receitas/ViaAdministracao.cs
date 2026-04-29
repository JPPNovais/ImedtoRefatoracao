namespace Imedto.Backend.Domain.Receitas;

/// <summary>Via de administração do medicamento prescrito.</summary>
public enum ViaAdministracao
{
    Oral,
    Topica,
    /// <summary>Intramuscular.</summary>
    IM,
    /// <summary>Endovenosa.</summary>
    EV,
    /// <summary>Subcutânea.</summary>
    SC,
    Outra
}
