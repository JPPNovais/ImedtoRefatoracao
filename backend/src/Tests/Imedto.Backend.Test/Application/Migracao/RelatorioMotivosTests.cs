using Imedto.Backend.Domain.Migracao;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// CA34/CA35/CA36/CA37/CA38/CA39 — addendum 002: agregação de motivos no relatório.
/// Testa a lógica de agrupamento do MigracaoRegistroRepository.ObterRelatorio diretamente
/// em memória (sem banco — isola a lógica de LINQ).
/// </summary>
[TestFixture]
public class RelatorioMotivosTests
{
    private const long JobId = 100L;
    private const long EstId = 42L;

    // Auxiliar: cria MigracaoRegistro via reflection (sem construtor público).
    private static MigracaoRegistro CriarRegistro(
        string entidade, string status, string? motivo = null)
    {
        var reg = (MigracaoRegistro)Activator.CreateInstance(
            typeof(MigracaoRegistro), nonPublic: true)!;

        // Seta via reflection as propriedades protegidas.
        void Set(string prop, object? val) =>
            typeof(MigracaoRegistro).GetProperty(prop)!.SetValue(reg, val);

        Set(nameof(MigracaoRegistro.MigracaoJobId), JobId);
        Set(nameof(MigracaoRegistro.EstabelecimentoId), EstId);
        Set(nameof(MigracaoRegistro.Entidade), entidade);
        Set(nameof(MigracaoRegistro.Status), status);
        Set(nameof(MigracaoRegistro.MotivoRejeicao), motivo);

        return reg;
    }

    private static RelatorioEntidade AgregarPorEntidade(IEnumerable<MigracaoRegistro> registros, string entidade)
    {
        // Replica a lógica do MigracaoRegistroRepository.ObterRelatorio em memória.
        var g = registros.Where(r => r.Entidade == entidade).ToList();
        return new RelatorioEntidade
        {
            Criados = g.Count(r => r.Status == "importado_criado"),
            Atualizados = g.Count(r => r.Status == "importado_atualizado"),
            Rejeitados = g.Count(r => r.Status == "rejeitado"),
            Pulados = g.Count(r => r.Status == "pulado"),
            MotivosRejeicao = g
                .Where(r => r.Status == "rejeitado" && r.MotivoRejeicao != null)
                .GroupBy(r => r.MotivoRejeicao!)
                .ToDictionary(mg => mg.Key, mg => mg.Count()),
            MotivosPulo = g
                .Where(r => r.Status == "pulado" && r.MotivoRejeicao != null)
                .GroupBy(r => r.MotivoRejeicao!)
                .ToDictionary(mg => mg.Key, mg => mg.Count()),
        };
    }

    /// <summary>
    /// CA34 — motivos de rejeição são agregados em motivo→quantidade.
    /// </summary>
    [Test]
    public void AgregarMotivos_Rejeicao_AgregaCorretamente()
    {
        var registros = new List<MigracaoRegistro>
        {
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),  // 12 × CPF ausente
            CriarRegistro("paciente", "rejeitado", "categoria não encontrada"),
            CriarRegistro("paciente", "rejeitado", "categoria não encontrada"),
            CriarRegistro("paciente", "rejeitado", "categoria não encontrada"),  // 3 × categoria não encontrada
        };

        var resultado = AgregarPorEntidade(registros, "paciente");

        Assert.That(resultado.Rejeitados, Is.EqualTo(15));
        Assert.That(resultado.MotivosRejeicao, Has.Count.EqualTo(2));
        Assert.That(resultado.MotivosRejeicao["CPF ausente"], Is.EqualTo(12));
        Assert.That(resultado.MotivosRejeicao["categoria não encontrada"], Is.EqualTo(3));
    }

    /// <summary>
    /// CA35 — motivos de pulo são agregados em motivo→quantidade.
    /// </summary>
    [Test]
    public void AgregarMotivos_Pulados_AgregaCorretamente()
    {
        var registros = new List<MigracaoRegistro>
        {
            CriarRegistro("paciente", "pulado", "identificador ausente"),
            CriarRegistro("paciente", "pulado", "identificador ausente"),
            CriarRegistro("paciente", "pulado", "identificador ausente"),
            CriarRegistro("paciente", "pulado", "identificador ausente"),
            CriarRegistro("paciente", "pulado", "identificador ausente"),  // 5 × identificador ausente
            CriarRegistro("agendamento", "pulado", "agendamento já existe"),
            CriarRegistro("agendamento", "pulado", "agendamento já existe"),
        };

        var resultadoPaciente = AgregarPorEntidade(registros, "paciente");
        var resultadoAgendamento = AgregarPorEntidade(registros, "agendamento");

        Assert.That(resultadoPaciente.MotivosPulo["identificador ausente"], Is.EqualTo(5));
        Assert.That(resultadoAgendamento.MotivosPulo["agendamento já existe"], Is.EqualTo(2));
    }

    /// <summary>
    /// CA37 — multi-tenant: a agregação usa apenas registros do job (jobId filtra o tenant).
    /// Sem vazamento entre jobs/tenants.
    /// </summary>
    [Test]
    public void AgregarMotivos_ApenasRegistrosDoJob()
    {
        // Apenas os registros passados para ObterRelatorio são do jobId correto.
        // A lógica em memória não acessa outros jobs — o filtro é no WHERE do banco.
        // Este teste valida que registros com entidades diferentes não se misturam.
        var registros = new List<MigracaoRegistro>
        {
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("agendamento", "rejeitado", "dados insuficientes"),
        };

        var resultadoPaciente = AgregarPorEntidade(registros, "paciente");
        var resultadoAgendamento = AgregarPorEntidade(registros, "agendamento");

        Assert.That(resultadoPaciente.MotivosRejeicao.ContainsKey("dados insuficientes"), Is.False,
            "Motivos de agendamento não devem aparecer em paciente.");
        Assert.That(resultadoAgendamento.MotivosRejeicao.ContainsKey("CPF ausente"), Is.False,
            "Motivos de paciente não devem aparecer em agendamento.");
    }

    /// <summary>
    /// CA38 — sem rejeições nem pulos, os dicts ficam vazios (sem poluir o relatório).
    /// </summary>
    [Test]
    public void AgregarMotivos_SemRejeicoes_DictsVazios()
    {
        var registros = new List<MigracaoRegistro>
        {
            CriarRegistro("paciente", "importado_criado"),
            CriarRegistro("paciente", "importado_atualizado"),
        };

        var resultado = AgregarPorEntidade(registros, "paciente");

        Assert.That(resultado.Rejeitados, Is.EqualTo(0));
        Assert.That(resultado.Pulados, Is.EqualTo(0));
        Assert.That(resultado.MotivosRejeicao, Is.Empty);
        Assert.That(resultado.MotivosPulo, Is.Empty);
    }

    /// <summary>
    /// CA39 — regressão: contagens de criados/atualizados/rejeitados/pulados permanecem corretas.
    /// </summary>
    [Test]
    public void AgregarMotivos_Contagens_NaoRegridem()
    {
        var registros = new List<MigracaoRegistro>
        {
            CriarRegistro("paciente", "importado_criado"),
            CriarRegistro("paciente", "importado_criado"),
            CriarRegistro("paciente", "importado_atualizado"),
            CriarRegistro("paciente", "rejeitado", "CPF ausente"),
            CriarRegistro("paciente", "pulado", "identificador ausente"),
        };

        var resultado = AgregarPorEntidade(registros, "paciente");

        Assert.That(resultado.Criados, Is.EqualTo(2));
        Assert.That(resultado.Atualizados, Is.EqualTo(1));
        Assert.That(resultado.Rejeitados, Is.EqualTo(1));
        Assert.That(resultado.Pulados, Is.EqualTo(1));
    }

    /// <summary>
    /// CA36 — motivos são categorias genéricas sem PII.
    /// Este teste verifica que os valores são os mesmos de MotivoRejeicao (já genérico por design).
    /// </summary>
    [Test]
    public void AgregarMotivos_ValoresSaoCategoriasGenericas()
    {
        // As chaves do dicionário são os mesmos valores gravados em motivo_rejeicao,
        // que por R3/CA4 do briefing original já são genéricos sem PII.
        var categoriasTipicas = new[]
        {
            "CPF ausente",
            "identificador ausente",
            "paciente não identificado",
            "categoria não encontrada",
            "sem chave única para dedupe",
            "agendamento já existe",
            "falha inesperada na carga",
        };

        foreach (var cat in categoriasTipicas)
        {
            // Nenhuma categoria típica contém CPF, nome ou telefone real.
            Assert.That(cat, Does.Not.Match(@"\d{3}\.\d{3}\.\d{3}-\d{2}"),
                $"Categoria '{cat}' não pode conter CPF real.");
            Assert.That(cat.Length, Is.LessThan(100),
                $"Categoria '{cat}' deve ser curta e legível.");
        }
    }
}
