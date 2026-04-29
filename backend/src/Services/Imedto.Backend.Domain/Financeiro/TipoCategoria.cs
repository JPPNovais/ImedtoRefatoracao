namespace Imedto.Backend.Domain.Financeiro;

/// <summary>
/// Tipo de uma categoria financeira. Apenas dois tipos são permitidos: receita ou despesa.
/// O valor é persistido como string (snake-case oficial do Postgres é o próprio nome do enum).
/// </summary>
public enum TipoCategoria
{
    Receita,
    Despesa
}
