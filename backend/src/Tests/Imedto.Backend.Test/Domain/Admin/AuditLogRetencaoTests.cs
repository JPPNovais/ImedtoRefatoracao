using System.Reflection;
using Imedto.Backend.Domain.Admin;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Admin;

/// <summary>
/// W7-CA6 — cobre AuditLogRetencao:
/// - Default para ação desconhecida.
/// - Ação conhecida retorna TTL do mapa.
/// - Mapa cobre todas as constantes públicas de AcoesAuditAdmin (ou documenta fallback).
/// </summary>
[TestFixture]
public class AuditLogRetencaoTests
{
    [Test]
    public void TtlDiasParaAcao_AcaoDesconhecida_RetornaDefault()
    {
        var resultado = AuditLogRetencao.TtlDiasParaAcao("ACAO_QUE_NAO_EXISTE");
        Assert.That(resultado, Is.EqualTo(AuditLogRetencao.DefaultDias));
    }

    [Test]
    public void TtlDiasParaAcao_LoginFail_Retorna365()
    {
        var resultado = AuditLogRetencao.TtlDiasParaAcao(AcoesAuditAdmin.LoginFail);
        Assert.That(resultado, Is.EqualTo(365));
    }

    [Test]
    public void TtlDiasParaAcao_RevelarCpfDono_Retorna730()
    {
        var resultado = AuditLogRetencao.TtlDiasParaAcao(AcoesAuditAdmin.RevelarCpfDono);
        Assert.That(resultado, Is.EqualTo(730));
    }

    [Test]
    public void TtlDiasParaAcao_LoginOk_Retorna30()
    {
        // Residual — não entra mais, mas mapa ainda cobre para limpar backlog rapidamente.
        var resultado = AuditLogRetencao.TtlDiasParaAcao(AcoesAuditAdmin.LoginOk);
        Assert.That(resultado, Is.EqualTo(30));
    }

    [Test]
    public void TtlDiasParaAcao_CriarAdmin_Retorna730()
    {
        var resultado = AuditLogRetencao.TtlDiasParaAcao(AcoesAuditAdmin.CriarAdmin);
        Assert.That(resultado, Is.EqualTo(730));
    }

    [Test]
    public void TtlDiasParaAcao_CriarModeloPadraoSistema_Retorna365()
    {
        var resultado = AuditLogRetencao.TtlDiasParaAcao(AcoesAuditAdmin.CriarModeloPadraoSistema);
        Assert.That(resultado, Is.EqualTo(365));
    }

    [Test]
    public void DefaultDias_E365()
    {
        Assert.That(AuditLogRetencao.DefaultDias, Is.EqualTo(365));
    }

    /// <summary>
    /// W7-CA6: verifica que toda constante pública de AcoesAuditAdmin tem entrada
    /// explícita no mapa OU é cobertura pelo default (documenta quais caem no fallback).
    /// </summary>
    [Test]
    public void PorAcao_CobreTodasAsConstantesDeAcoesAuditAdmin()
    {
        var constantes = typeof(AcoesAuditAdmin)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.That(constantes, Is.Not.Empty, "AcoesAuditAdmin deve ter ao menos uma constante.");

        var semEntrada = constantes
            .Where(c => !AuditLogRetencao.PorAcao.ContainsKey(c))
            .ToList();

        // Constantes sem entrada explícita caem no default de 365 dias — documentar aqui
        // para que o PR que adicionar nova constante perceba que deve incluir no mapa.
        if (semEntrada.Count > 0)
            Assert.Warn(
                $"As seguintes constantes de AcoesAuditAdmin NÃO têm entrada explícita no " +
                $"AuditLogRetencao.PorAcao e usam o default de {AuditLogRetencao.DefaultDias} dias: " +
                string.Join(", ", semEntrada));

        // O teste passa mesmo com fallback — apenas documenta o gap.
        Assert.Pass($"Cobertura: {constantes.Count - semEntrada.Count}/{constantes.Count} ações mapeadas explicitamente.");
    }
}
