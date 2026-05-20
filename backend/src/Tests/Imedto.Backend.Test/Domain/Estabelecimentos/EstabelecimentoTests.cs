using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.SharedKernel.Domain;
using NUnit.Framework;

namespace Imedto.Backend.Test.Domain.Estabelecimentos;

[TestFixture]
public class EstabelecimentoTests
{
    private static Estabelecimento CriarValido() =>
        Estabelecimento.Criar(
            donoUsuarioId: Guid.NewGuid(),
            nomeFantasia: "Clinica Imedto",
            razaoSocial: "Imedto LTDA",
            cnpj: "12.345.678/0001-95",
            telefone: "(11) 99999-8888",
            endereco: "Rua A, 123");

    // ----- Criar -----

    [Test]
    public void Criar_Valido_DefaultsCorretos()
    {
        var e = CriarValido();

        Assert.That(e.Status, Is.EqualTo(EstabelecimentoStatus.Ativo));
        Assert.That(e.HorarioInicio, Is.EqualTo(new TimeOnly(8, 0)));
        Assert.That(e.HorarioFim, Is.EqualTo(new TimeOnly(18, 0)));
        Assert.That(e.DiasSemanaFuncionamento, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
        Assert.That(e.HorariosBloqueados, Is.Empty);
        Assert.That(e.DatasBloqueadas, Is.Empty);
        Assert.That(e.CriadoEm, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Criar_NormalizaCnpjETelefone_SomenteDigitos()
    {
        var e = CriarValido();
        Assert.That(e.Cnpj, Is.EqualTo("12345678000195"));
        Assert.That(e.Telefone, Is.EqualTo("11999998888"));
    }

    [Test]
    public void Criar_NomeFantasiaTrim_RemoveEspacos()
    {
        var e = Estabelecimento.Criar(
            Guid.NewGuid(), "  Clinica  ", null, null, null, null);
        Assert.That(e.NomeFantasia, Is.EqualTo("Clinica"));
    }

    [Test]
    public void Criar_DonoGuidEmpty_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Estabelecimento.Criar(Guid.Empty, "Clinica", null, null, null, null));
        Assert.That(ex.Message, Does.Contain("Dono"));
    }

    [Test]
    public void Criar_NomeFantasiaVazio_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Estabelecimento.Criar(Guid.NewGuid(), "  ", null, null, null, null));
        Assert.That(ex.Message, Does.Contain("Nome fantasia"));
    }

    [Test]
    public void Criar_CnpjComprimentoInvalido_LancaBusinessException()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            Estabelecimento.Criar(Guid.NewGuid(), "Clinica", null, "12345", null, null));
        Assert.That(ex.Message, Does.Contain("14 dígitos"));
    }

    [Test]
    public void Criar_SemCnpj_PermiteNull()
    {
        var e = Estabelecimento.Criar(Guid.NewGuid(), "Clinica", null, null, null, null);
        Assert.That(e.Cnpj, Is.Null);
    }

    // ----- MarcarComoCriado -----

    [Test]
    public void MarcarComoCriado_IdZero_LancaInvalidOperation()
    {
        var e = CriarValido();
        Assert.Throws<InvalidOperationException>(() => e.MarcarComoCriado());
    }

    [Test]
    public void MarcarComoCriado_IdValido_PublicaEvento()
    {
        var e = CriarValido();
        // Simular persistência do EF setando Id via reflection (Id é protected set).
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(e, 42L);

        e.MarcarComoCriado();

        Assert.That(e.DomainEvents, Has.Count.EqualTo(1));
        Assert.That(e.DomainEvents.First(), Is.TypeOf<EstabelecimentoCriadoEvent>());
    }

    // ----- AtualizarDados -----

    [Test]
    public void AtualizarDados_Valido_AtualizaCampos()
    {
        var e = CriarValido();
        e.AtualizarDados("Nova Clinica", "Nova LTDA", "98.765.432/0001-10", "11888887777", "Rua B");

        Assert.That(e.NomeFantasia, Is.EqualTo("Nova Clinica"));
        Assert.That(e.RazaoSocial, Is.EqualTo("Nova LTDA"));
        Assert.That(e.Cnpj, Is.EqualTo("98765432000110"));
        Assert.That(e.Telefone, Is.EqualTo("11888887777"));
        Assert.That(e.Endereco, Is.EqualTo("Rua B"));
        Assert.That(e.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void AtualizarDados_NomeVazio_LancaBusinessException()
    {
        var e = CriarValido();
        var ex = Assert.Throws<BusinessException>(() =>
            e.AtualizarDados("  ", null, null, null, null));
        Assert.That(ex.Message, Does.Contain("Nome fantasia"));
    }

    [Test]
    public void AtualizarDados_CnpjInvalido_LancaBusinessException()
    {
        var e = CriarValido();
        var ex = Assert.Throws<BusinessException>(() =>
            e.AtualizarDados("Clinica", null, "12345", null, null));
        Assert.That(ex.Message, Does.Contain("14 dígitos"));
    }

    // ----- AtualizarFuncionamento -----

    [Test]
    public void AtualizarFuncionamento_Valido_AtualizaConfiguracao()
    {
        var e = CriarValido();
        var bloqueio = new HorarioBloqueado(Guid.Empty, new TimeOnly(12, 0), new TimeOnly(13, 0), "Almoço");
        var dataBloqueada = new DataBloqueada(Guid.Empty, new DateOnly(2026, 12, 25), "Natal");

        e.AtualizarFuncionamento(
            new TimeOnly(7, 0),
            new TimeOnly(19, 0),
            45,
            10,
            new[] { 1, 2, 3, 4, 5, 6 },
            new[] { bloqueio },
            new[] { dataBloqueada });

        Assert.That(e.HorarioInicio, Is.EqualTo(new TimeOnly(7, 0)));
        Assert.That(e.HorarioFim, Is.EqualTo(new TimeOnly(19, 0)));
        Assert.That(e.DuracaoConsultaPadraoMinutos, Is.EqualTo(45));
        Assert.That(e.IntervaloEntreConsultasMinutos, Is.EqualTo(10));
        Assert.That(e.DiasSemanaFuncionamento, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(e.HorariosBloqueados, Has.Count.EqualTo(1));
        Assert.That(e.HorariosBloqueados[0].Id, Is.Not.EqualTo(Guid.Empty), "Id Guid.Empty deve ser substituído.");
        Assert.That(e.DatasBloqueadas, Has.Count.EqualTo(1));
        Assert.That(e.DatasBloqueadas[0].Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void AtualizarFuncionamento_HorarioFimAntesInicio_LancaBusinessException()
    {
        var e = CriarValido();
        var ex = Assert.Throws<BusinessException>(() =>
            e.AtualizarFuncionamento(
                new TimeOnly(18, 0), new TimeOnly(8, 0),
                30, 0,
                new[] { 1 },
                Array.Empty<HorarioBloqueado>(),
                Array.Empty<DataBloqueada>()));
        Assert.That(ex.Message, Does.Contain("término"));
    }

    [Test]
    public void AtualizarFuncionamento_DuracaoInvalida_LancaBusinessException()
    {
        var e = CriarValido();
        var ex = Assert.Throws<BusinessException>(() =>
            e.AtualizarFuncionamento(
                new TimeOnly(8, 0), new TimeOnly(18, 0),
                0, 0,
                new[] { 1 },
                Array.Empty<HorarioBloqueado>(),
                Array.Empty<DataBloqueada>()));
        Assert.That(ex.Message, Does.Contain("Duração"));
    }

    [Test]
    public void AtualizarFuncionamento_SemDias_LancaBusinessException()
    {
        var e = CriarValido();
        var ex = Assert.Throws<BusinessException>(() =>
            e.AtualizarFuncionamento(
                new TimeOnly(8, 0), new TimeOnly(18, 0),
                30, 0,
                Array.Empty<int>(),
                Array.Empty<HorarioBloqueado>(),
                Array.Empty<DataBloqueada>()));
        Assert.That(ex.Message, Does.Contain("dia"));
    }

    [Test]
    public void AtualizarFuncionamento_DiaForaDoIntervalo_LancaBusinessException()
    {
        var e = CriarValido();
        var ex = Assert.Throws<BusinessException>(() =>
            e.AtualizarFuncionamento(
                new TimeOnly(8, 0), new TimeOnly(18, 0),
                30, 0,
                new[] { 7 },
                Array.Empty<HorarioBloqueado>(),
                Array.Empty<DataBloqueada>()));
        Assert.That(ex.Message, Does.Contain("Dia da semana"));
    }

    [Test]
    public void AtualizarFuncionamento_BloqueioForaDoFuncionamento_LancaBusinessException()
    {
        var e = CriarValido();
        var bloqueio = new HorarioBloqueado(Guid.Empty, new TimeOnly(7, 0), new TimeOnly(9, 0), "Fora");
        var ex = Assert.Throws<BusinessException>(() =>
            e.AtualizarFuncionamento(
                new TimeOnly(8, 0), new TimeOnly(18, 0),
                30, 0,
                new[] { 1 },
                new[] { bloqueio },
                Array.Empty<DataBloqueada>()));
        Assert.That(ex.Message, Does.Contain("dentro do horário"));
    }

    [Test]
    public void AtualizarFuncionamento_DatasDuplicadas_LancaBusinessException()
    {
        var e = CriarValido();
        var data = new DateOnly(2026, 12, 25);
        var d1 = new DataBloqueada(Guid.Empty, data, "Natal");
        var d2 = new DataBloqueada(Guid.Empty, data, "Duplicada");
        var ex = Assert.Throws<BusinessException>(() =>
            e.AtualizarFuncionamento(
                new TimeOnly(8, 0), new TimeOnly(18, 0),
                30, 0,
                new[] { 1 },
                Array.Empty<HorarioBloqueado>(),
                new[] { d1, d2 }));
        Assert.That(ex.Message, Does.Contain("duplicada"));
    }

    // ----- ValidarPodeAgendar -----

    [Test]
    public void ValidarPodeAgendar_DentroDoHorario_NaoLanca()
    {
        var e = CriarValido();
        // Próxima segunda-feira (dia da semana 1) às 10h.
        var segunda = ProximoDiaDaSemana(DayOfWeek.Monday).Date.AddHours(10);
        var fim = segunda.AddMinutes(30);
        var agora = segunda.AddDays(-1);
        Assert.DoesNotThrow(() => e.ValidarPodeAgendar(segunda, fim, agora));
    }

    [Test]
    public void ValidarPodeAgendar_DiaForaDoFuncionamento_LancaBusinessException()
    {
        var e = CriarValido();
        var domingo = ProximoDiaDaSemana(DayOfWeek.Sunday).Date.AddHours(10);
        var fim = domingo.AddMinutes(30);
        var agora = domingo.AddDays(-1);
        var ex = Assert.Throws<BusinessException>(() => e.ValidarPodeAgendar(domingo, fim, agora));
        Assert.That(ex.Message, Does.Contain("dia da semana"));
    }

    [Test]
    public void ValidarPodeAgendar_ForaDoHorario_LancaBusinessException()
    {
        var e = CriarValido();
        var segunda = ProximoDiaDaSemana(DayOfWeek.Monday).Date.AddHours(20);
        var fim = segunda.AddMinutes(30);
        var agora = segunda.AddDays(-1);
        var ex = Assert.Throws<BusinessException>(() => e.ValidarPodeAgendar(segunda, fim, agora));
        Assert.That(ex.Message, Does.Contain("horário de funcionamento"));
    }

    [Test]
    public void ValidarPodeAgendar_NoPassado_LancaBusinessException()
    {
        var e = CriarValido();
        var segunda = ProximoDiaDaSemana(DayOfWeek.Monday).Date.AddHours(10);
        var fim = segunda.AddMinutes(30);
        // "Agora" depois do início → início está no passado.
        var agora = segunda.AddDays(1);
        var ex = Assert.Throws<BusinessException>(() => e.ValidarPodeAgendar(segunda, fim, agora));
        Assert.That(ex.Message, Does.Contain("passado"));
    }

    [Test]
    public void ValidarPodeAgendar_FimUltrapassaExpediente_LancaBusinessException()
    {
        var e = CriarValido();
        // Estabelecimento padrão funciona até 18:00. Inicia 17:30 com 1h → ultrapassa.
        var segunda = ProximoDiaDaSemana(DayOfWeek.Monday).Date.AddHours(17).AddMinutes(30);
        var fim = segunda.AddHours(1);
        var agora = segunda.AddDays(-1);
        var ex = Assert.Throws<BusinessException>(() => e.ValidarPodeAgendar(segunda, fim, agora));
        Assert.That(ex.Message, Does.Contain("ultrapassa"));
    }

    // ----- AlterarFoto -----

    [Test]
    public void AlterarFoto_UrlValida_AtualizaFoto()
    {
        var e = CriarValido();
        e.AlterarFoto("https://cdn.imedto.com/foto.png");
        Assert.That(e.FotoUrl, Is.EqualTo("https://cdn.imedto.com/foto.png"));
        Assert.That(e.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void AlterarFoto_UrlVazia_LancaBusinessException()
    {
        var e = CriarValido();
        Assert.Throws<BusinessException>(() => e.AlterarFoto("  "));
    }

    // ----- Inativar / Reativar -----

    [Test]
    public void Inativar_StatusAtivo_VolvePraInativo()
    {
        var e = CriarValido();
        e.Inativar();
        Assert.That(e.Status, Is.EqualTo(EstabelecimentoStatus.Inativo));
    }

    [Test]
    public void Inativar_JaInativo_LancaBusinessException()
    {
        var e = CriarValido();
        e.Inativar();
        Assert.Throws<BusinessException>(() => e.Inativar());
    }

    [Test]
    public void Reativar_StatusInativo_VolveParaAtivo()
    {
        var e = CriarValido();
        e.Inativar();
        e.Reativar();
        Assert.That(e.Status, Is.EqualTo(EstabelecimentoStatus.Ativo));
    }

    [Test]
    public void Reativar_JaAtivo_LancaBusinessException()
    {
        var e = CriarValido();
        Assert.Throws<BusinessException>(() => e.Reativar());
    }

    private static DateTime ProximoDiaDaSemana(DayOfWeek alvo)
    {
        var hoje = DateTime.Today;
        var diff = ((int)alvo - (int)hoje.DayOfWeek + 7) % 7;
        return hoje.AddDays(diff == 0 ? 7 : diff);
    }

    // ----- AtualizarEndereco (Fase 1 Termos — 2026-05-19) -----

    [Test]
    public void AtualizarEndereco_CamposValidos_PopulaCidadeEUf()
    {
        var e = CriarValido();
        e.AtualizarEndereco("Rua Nova, 50", "São Paulo", "sp");

        Assert.That(e.Endereco, Is.EqualTo("Rua Nova, 50"));
        Assert.That(e.Cidade, Is.EqualTo("São Paulo"));
        Assert.That(e.Estado, Is.EqualTo("SP"));
        Assert.That(e.AtualizadoEm, Is.Not.Null);
    }

    [Test]
    public void AtualizarEndereco_CamposVazios_PersistemNulo()
    {
        var e = CriarValido();
        e.AtualizarEndereco("", "  ", null);

        Assert.That(e.Endereco, Is.Null);
        Assert.That(e.Cidade, Is.Null);
        Assert.That(e.Estado, Is.Null);
    }

    [Test]
    public void AtualizarEndereco_UfComMaisDeDuasLetras_LancaBusinessException()
    {
        var e = CriarValido();
        Assert.Throws<BusinessException>(() => e.AtualizarEndereco("Rua", "Cidade", "SAO"));
    }

    [Test]
    public void AtualizarEndereco_UfComDigitos_LancaBusinessException()
    {
        var e = CriarValido();
        Assert.Throws<BusinessException>(() => e.AtualizarEndereco("Rua", "Cidade", "S1"));
    }
}
