namespace Imedto.Backend.Domain.Orcamentos;

/// <summary>
/// Tipo do orçamento. <c>Simples</c> mantém compatibilidade com o aggregate original
/// (apenas itens linha-a-linha). <c>Cirurgico</c> ativa equipe (com comissão), implantes
/// e referência opcional a um <c>ProcedimentoCirurgico</c>.
/// </summary>
public enum TipoOrcamento
{
    Simples,
    Cirurgico
}
