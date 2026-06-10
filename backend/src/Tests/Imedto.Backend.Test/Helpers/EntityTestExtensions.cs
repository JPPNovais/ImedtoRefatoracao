using System.Reflection;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Test.Helpers;

/// <summary>
/// Extensões para simular estado de entidades persistidas nos testes de handler.
/// Necessário porque Entity.Id tem protected set (design correto de DDD), mas
/// testes de handler precisam de entidades com Id > 0 para validações de domínio.
/// </summary>
public static class EntityTestExtensions
{
    /// <summary>
    /// Simula o Id que seria atribuído pelo banco após persistência.
    /// Uso restrito a testes — não usar em código de produção.
    /// </summary>
    public static T SimularIdBanco<T>(this T entity, long id) where T : Entity
    {
        // Sobe a hierarquia até encontrar a propriedade Id com setter protegido.
        var tipo = typeof(T);
        PropertyInfo? prop = null;
        while (tipo != null && prop == null)
        {
            prop = tipo.GetProperty("Id",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            tipo = tipo.BaseType;
        }
        prop?.SetValue(entity, id);
        return entity;
    }
}
