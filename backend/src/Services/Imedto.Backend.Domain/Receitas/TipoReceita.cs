namespace Imedto.Backend.Domain.Receitas;

/// <summary>
/// Categoria fiscal/regulatória da receita. Receitas <see cref="Controlada"/> seguem
/// regras da ANVISA (Portaria 344/98) — exigem validade, livro de registro e numeração
/// específica. As demais são clínicas comuns. <see cref="Antibiotico"/> tem
/// receituário próprio (RDC 471/2021) e <see cref="Especial"/> cobre fluxos
/// fora-do-padrão definidos pelo estabelecimento (ex.: oftalmológica/odontológica).
/// </summary>
public enum TipoReceita
{
    Comum,
    Controlada,
    Antibiotico,
    Especial
}
