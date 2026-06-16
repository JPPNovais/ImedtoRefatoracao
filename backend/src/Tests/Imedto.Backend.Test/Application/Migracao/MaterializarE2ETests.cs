using System.Text.Json;
using Imedto.Backend.Application.Admin.Migracao;
using Imedto.Backend.Application.Migracao.Jobs;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Imedto.Backend.Test.Application.Migracao;

/// <summary>
/// CA105 — Teste E2E que comprova INSERÇÃO REAL via fluxo completo:
///   inferência (IA mockada) → materialização → carga → N criados ≠ 0.
///
/// Monta um arquivo ZIP com dump JSON contendo pacientes + agendamentos.
/// IA mockada retorna um de-para com CHAVES CANÔNICAS REAIS (as que a IA produziria).
/// Materializa os registros pendentes.
/// Roda a carga (CarregarOnda1JobHandler).
/// Verifica que Paciente e Agendamento foram REALMENTE criados via Salvar nos repos.
/// Verifica que o relatório reporta N criados ≠ 0.
///
/// Este teste documenta e prova os bugs corrigidos no job #14:
///   Bug #1: carga lia "nome_completo" mas canônico é "nome".
///   Bug #2: carga lia "genero" mas canônico é "sexo".
///   Bug #3: carga passava Guid.Empty como criadoPorUsuarioId — rejeição pela invariante.
///   Bug #4: carga lia "paciente_id"/"profissional_usuario_id" — IDs internos nunca presentes
///           no payload canônico; reescrito para usar "paciente_nome"/"profissional_nome"/"data_hora".
/// </summary>
[TestFixture]
public class MaterializarE2ETests
{
    private const long EstabelecimentoId = 42;
    private const long JobId             = 200;
    private static readonly Guid AdminId = Guid.NewGuid();

    // Guid do profissional no tenant de teste
    private static readonly Guid ProfissionalId = Guid.NewGuid();
    // paciente criado pelo mock do Salvar
    private const long PacienteIdFicto = 1001L;

    // ─── Setup dos mocks ─────────────────────────────────────────────────────────

    private Mock<IMigracaoJobRepository>          _jobRepo;
    private Mock<IMigracaoMapaRepository>         _mapaRepo;
    private Mock<IMigracaoRegistroRepository>     _registroRepo;
    private Mock<IMigracaoArquivoStorageService>  _storage;
    private Mock<IMigracaoArquivoParser>          _parser;
    private Mock<IMigracaoJobEventoRepository>    _eventoRepo;
    private Mock<IPacienteRepository>             _pacienteRepo;
    private Mock<IAgendamentoRepository>          _agendamentoRepo;
    private Mock<ICategoriaEstoqueRepository>     _categoriaRepo;
    private Mock<IFabricanteEstoqueRepository>    _fabricanteRepo;
    private Mock<IFornecedorEstoqueRepository>    _fornecedorRepo;
    private Mock<ILocalEstoqueRepository>         _localRepo;
    private Mock<IItemInventarioRepository>       _itemRepo;
    private Mock<ICatalogoCirurgiaRepository>     _cirurgiaRepo;
    private Mock<ICatalogoProdutoRepository>      _produtoRepo;
    private Mock<IMigracaoCatalogoCirurgiaLookup> _cirurgiaLookup;
    private Mock<IMigracaoCatalogoProdutoLookup>  _produtoLookup;
    private Mock<IMigracaoPacienteLookup>         _pacienteLookup;
    private Mock<IMigracaoAgendamentoLookup>      _agendamentoLookup;

    // Registros em memória — rastreados entre materialização e carga
    private readonly List<MigracaoRegistro> _registrosEmMemoria = [];

    [SetUp]
    public void SetUp()
    {
        _jobRepo           = new Mock<IMigracaoJobRepository>();
        _mapaRepo          = new Mock<IMigracaoMapaRepository>();
        _registroRepo      = new Mock<IMigracaoRegistroRepository>();
        _storage           = new Mock<IMigracaoArquivoStorageService>();
        _parser            = new Mock<IMigracaoArquivoParser>();
        _eventoRepo        = new Mock<IMigracaoJobEventoRepository>();
        _pacienteRepo      = new Mock<IPacienteRepository>();
        _agendamentoRepo   = new Mock<IAgendamentoRepository>();
        _categoriaRepo     = new Mock<ICategoriaEstoqueRepository>();
        _fabricanteRepo    = new Mock<IFabricanteEstoqueRepository>();
        _fornecedorRepo    = new Mock<IFornecedorEstoqueRepository>();
        _localRepo         = new Mock<ILocalEstoqueRepository>();
        _itemRepo          = new Mock<IItemInventarioRepository>();
        _cirurgiaRepo      = new Mock<ICatalogoCirurgiaRepository>();
        _produtoRepo       = new Mock<ICatalogoProdutoRepository>();
        _cirurgiaLookup    = new Mock<IMigracaoCatalogoCirurgiaLookup>();
        _produtoLookup     = new Mock<IMigracaoCatalogoProdutoLookup>();
        _pacienteLookup    = new Mock<IMigracaoPacienteLookup>();
        _agendamentoLookup = new Mock<IMigracaoAgendamentoLookup>();

        _registrosEmMemoria.Clear();

        // Evento repo — aceita tudo
        _eventoRepo.Setup(r => r.Gravar(It.IsAny<MigracaoJobEvento>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        // DELETE pendentes — limpa a lista em memória
        _registroRepo.Setup(r => r.DeletarPendentesPorJob(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                     .Callback<long, CancellationToken>((jobId, _) =>
                         _registrosEmMemoria.RemoveAll(r => r.MigracaoJobId == jobId && r.Status == "pendente"))
                     .Returns(Task.CompletedTask);

        // SalvarLote — adiciona à lista em memória e atribui IDs fictícios
        _registroRepo.Setup(r => r.SalvarLote(It.IsAny<IReadOnlyList<MigracaoRegistro>>(), It.IsAny<CancellationToken>()))
                     .Callback<IReadOnlyList<MigracaoRegistro>, CancellationToken>((lista, _) =>
                     {
                         var idx = _registrosEmMemoria.Count;
                         foreach (var reg in lista)
                         {
                             typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(reg, (long)(++idx));
                             _registrosEmMemoria.Add(reg);
                         }
                     })
                     .Returns(Task.CompletedTask);

        // Salvar individual — aceita
        _registroRepo.Setup(r => r.Salvar(It.IsAny<MigracaoRegistro>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

        // ListarPorJob — retorna a lista em memória
        _registroRepo.Setup(r => r.ListarPorJob(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(() => _registrosEmMemoria.Where(r => r.MigracaoJobId == JobId).ToList());

        // ObterRelatorio — lê a lista em memória
        _registroRepo.Setup(r => r.ObterRelatorio(JobId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(() =>
                     {
                         var todos = _registrosEmMemoria.Where(r => r.MigracaoJobId == JobId).ToList();
                         return new RelatorioMigracao
                         {
                             TotalCriados    = todos.Count(r => r.Status == "importado_criado"),
                             TotalAtualizados = todos.Count(r => r.Status == "importado_atualizado"),
                             TotalRejeitados = todos.Count(r => r.Status == "rejeitado"),
                             TotalPulados    = todos.Count(r => r.Status == "pulado"),
                             PorEntidade     = todos.GroupBy(r => r.Entidade).ToDictionary(
                                 g => g.Key,
                                 g => new RelatorioEntidade
                                 {
                                     Criados    = g.Count(r => r.Status == "importado_criado"),
                                     Rejeitados = g.Count(r => r.Status == "rejeitado"),
                                 }),
                         };
                     });

        // Paciente — não existe por CPF → cria; Salvar seta ID fictício
        _pacienteRepo.Setup(r => r.ObterPorCpfOuNulo(It.IsAny<string>(), EstabelecimentoId))
                     .ReturnsAsync((Paciente?)null);
        _pacienteRepo.Setup(r => r.Salvar(It.IsAny<Paciente>()))
                     .Callback<Paciente>(p => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, PacienteIdFicto))
                     .Returns(Task.CompletedTask);

        // PacienteLookup — lookup por CPF retorna null (paciente ainda não existe no inicio),
        // lookup por nome retorna PacienteIdFicto (simula: paciente já foi criado antes do agendamento,
        // pois a ordem FK garante pacientes antes de agendamentos).
        _pacienteLookup.Setup(r => r.ObterPorCpfOuNulo(It.IsAny<string>(), EstabelecimentoId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PacienteMigracaoInfo?)null);
        _pacienteLookup.Setup(r => r.ObterIdPorNomeOuNulo("Ana Paula", EstabelecimentoId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(PacienteIdFicto);

        // AgendamentoLookup — profissional encontrado por nome
        _agendamentoLookup.Setup(r => r.ObterProfissionalIdPorNomeOuNulo("Dr. João Silva", EstabelecimentoId, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(ProfissionalId);

        // Agendamento — não existe → cria
        _agendamentoRepo.Setup(r => r.ObterPorChaveDeNegocioOuNulo(
                             It.IsAny<long>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), EstabelecimentoId))
                        .ReturnsAsync((Agendamento?)null);
        _agendamentoRepo.Setup(r => r.Salvar(It.IsAny<Agendamento>())).Returns(Task.CompletedTask);

        // Repos de estoque/catálogos — nunca chamados neste E2E (apenas paciente+agenda)
        _categoriaRepo.Setup(r => r.ObterPorNomeOuNulo(It.IsAny<string>(), It.IsAny<long>()))
                      .ReturnsAsync((CategoriaEstoque?)null);
        _cirurgiaLookup.Setup(r => r.ObterIdPorCodigoOuNulo(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((long?)null);
        _cirurgiaLookup.Setup(r => r.ObterIdPorNomeOuNulo(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((long?)null);
        _produtoLookup.Setup(r => r.ObterIdPorCodigoOuNulo(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((long?)null);
        _produtoLookup.Setup(r => r.ObterIdPorNomeOuNulo(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((long?)null);
    }

    // ─── CA105 — fluxo completo ponta a ponta com chaves canônicas REAIS ──────────

    [Test]
    public async Task CA105_FluxoCompleto_PacienteEAgendamentoCriados_RelatorioNaoZero()
    {
        // ── 1. Montar o job ──────────────────────────────────────────────────────

        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid(), "Sistema Legado");
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        job.RegistrarArquivoRecebido("migracao/42/200/dump.zip");
        job.AprovarAnalise(AdminId);
        job.MarcarMapaEmRevisao();

        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _jobRepo.Setup(r => r.Salvar(It.IsAny<MigracaoJob>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // ── 2. Dados do dump — colunas de ORIGEM (sistema legado) ───────────────

        var linhaPaciente = new Dictionary<string, string>
        {
            // Colunas do sistema de origem (nomes arbitrários do cliente)
            ["nome_cliente"]   = "Ana Paula",
            ["cpf_paciente"]   = "529.982.247-25",  // CPF com dígitos verificadores válidos
            ["sexo_paciente"]  = "Feminino",
        };

        // Agendamento histórico (passado — data proposital para testar CriarHistorico)
        var dataHistorica = DateTime.UtcNow.AddMonths(-3).ToString("yyyy-MM-dd HH:mm");
        var linhaAgendamento = new Dictionary<string, string>
        {
            ["nome_paciente"]    = "Ana Paula",
            ["medico_nome"]      = "Dr. João Silva",
            ["data_consulta"]    = dataHistorica,
            ["duracao"]          = "45",
            ["tipo_atendimento"] = "Retorno",
        };

        var blocoPacientes    = new BlocoCandidato
        {
            NomeBloco  = "pacientes",
            Cabecalhos = ["nome_cliente", "cpf_paciente", "sexo_paciente"],
            Linhas     = [(IReadOnlyDictionary<string, string>)linhaPaciente],
        };
        var blocoAgendamentos = new BlocoCandidato
        {
            NomeBloco  = "agendamentos",
            Cabecalhos = ["nome_paciente", "medico_nome", "data_consulta", "duracao", "tipo_atendimento"],
            Linhas     = [(IReadOnlyDictionary<string, string>)linhaAgendamento],
        };

        // ── 3. Parser retorna os dois blocos ────────────────────────────────────

        _parser.Setup(p => p.SuportaFormato(It.IsAny<string>())).Returns(true);
        _parser.Setup(p => p.ParsearAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ArquivoParseado
               {
                   Cabecalhos = ["nome_cliente"],
                   Linhas     = [linhaPaciente],
                   Blocos     = [blocoPacientes, blocoAgendamentos],
               });

        var zipStream = CriarZipComEntrada("dump.json", "{}"u8.ToArray());
        _storage.Setup(s => s.DownloadArquivoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(zipStream);

        // ── 4. Mapas com CHAVES CANÔNICAS REAIS ─────────────────────────────────
        // O de-para reflete o que a IA produziria: coluna_origem → campo_canônico.
        // Canônicos de paciente: nome, cpf, sexo, data_nascimento, telefone, email, etc.
        // Canônicos de agendamento: paciente_nome, profissional_nome, data_hora, duracao_minutos, tipo_consulta.

        var mapaPacienteJson = JsonSerializer.Serialize(new
        {
            de_para = new Dictionary<string, string>
            {
                ["nome_cliente"]  = "nome",       // bug #1 corrigido: canônico é "nome", não "nome_completo"
                ["cpf_paciente"]  = "cpf",
                ["sexo_paciente"] = "sexo",       // bug #2 corrigido: canônico é "sexo", não "genero"
            },
            ignorado  = false,
            eh_config = false,
        });
        var mapaPaciente = MigracaoMapa.Criar(JobId, EstabelecimentoId, "paciente", mapaPacienteJson, "pacientes");

        var mapaAgendamentoJson = JsonSerializer.Serialize(new
        {
            de_para = new Dictionary<string, string>
            {
                // bug #4 corrigido: canônicos textuais, não IDs internos
                ["nome_paciente"]    = "paciente_nome",
                ["medico_nome"]      = "profissional_nome",
                ["data_consulta"]    = "data_hora",
                ["duracao"]          = "duracao_minutos",
                ["tipo_atendimento"] = "tipo_consulta",
            },
            ignorado  = false,
            eh_config = false,
        });
        var mapaAgendamento = MigracaoMapa.Criar(JobId, EstabelecimentoId, "agendamento", mapaAgendamentoJson, "agendamentos");

        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<MigracaoMapa> { mapaPaciente, mapaAgendamento });

        // ── 5. Materializar ──────────────────────────────────────────────────────

        var materializarHandler = new MaterializarRegistrosCommandHandler(
            _jobRepo.Object,
            _mapaRepo.Object,
            _registroRepo.Object,
            _storage.Object,
            new[] { _parser.Object },
            NullLogger<MaterializarRegistrosCommandHandler>.Instance);

        await materializarHandler.ExecutarAsync(JobId);

        // Verifica: 2 registros pendentes foram criados (1 paciente + 1 agendamento)
        var pendentes = _registrosEmMemoria.Where(r => r.Status == "pendente").ToList();
        Assert.That(pendentes.Count, Is.EqualTo(2), "CA105: materialização deve criar 2 registros pendentes");
        Assert.That(pendentes.Any(r => r.Entidade == "paciente"), Is.True);
        Assert.That(pendentes.Any(r => r.Entidade == "agendamento"), Is.True);

        // Verifica que o payload do paciente usa chaves canônicas (não "nome_completo")
        var regPaciente = pendentes.First(r => r.Entidade == "paciente");
        var payloadPaciente = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(regPaciente.PayloadBruto)!;
        Assert.That(payloadPaciente.ContainsKey("nome"), Is.True,
            "CA105/Bug#1: payload do paciente deve ter chave 'nome' (canônico), não 'nome_completo'");
        Assert.That(payloadPaciente.ContainsKey("sexo"), Is.True,
            "CA105/Bug#2: payload do paciente deve ter chave 'sexo' (canônico), não 'genero'");
        Assert.That(payloadPaciente.ContainsKey("nome_completo"), Is.False,
            "CA105/Bug#1: 'nome_completo' não deve aparecer — é chave de origem, não canônica");

        // Verifica que o payload do agendamento usa chaves canônicas textuais
        var regAgendamento = pendentes.First(r => r.Entidade == "agendamento");
        var payloadAgendamento = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(regAgendamento.PayloadBruto)!;
        Assert.That(payloadAgendamento.ContainsKey("data_hora"), Is.True,
            "CA105/Bug#4: payload do agendamento deve ter 'data_hora' (canônico)");
        Assert.That(payloadAgendamento.ContainsKey("profissional_nome"), Is.True,
            "CA105/Bug#4: payload do agendamento deve ter 'profissional_nome' (canônico)");
        Assert.That(payloadAgendamento.ContainsKey("paciente_nome"), Is.True,
            "CA105/Bug#4: payload do agendamento deve ter 'paciente_nome' (canônico)");
        Assert.That(payloadAgendamento.ContainsKey("paciente_id"), Is.False,
            "CA105/Bug#4: 'paciente_id' não deve aparecer — é ID interno, nunca produzido pela IA");

        // ── 6. Avançar job para migrando ────────────────────────────────────────

        job.MarcarPreviewPronto(AdminId);
        job.MarcarMigrando(AdminId);
        _jobRepo.Setup(r => r.ObterMaisAntigoMigrandoOuNulo(It.IsAny<CancellationToken>())).ReturnsAsync(job);

        // ── 7. Carregar Onda 1 ───────────────────────────────────────────────────

        var carregarHandler = CriarCarregarHandler();

        await carregarHandler.ExecutarAsync(default);

        // ── 8. Verificar que entidades de domínio foram REALMENTE criadas ────────

        // Bug #1 corrigido: paciente deve ser criado (não mais rejeitado por "nome obrigatório")
        _pacienteRepo.Verify(
            r => r.Salvar(It.IsAny<Paciente>()),
            Times.Once,
            "CA105/Bug#1: Paciente deve ter sido REALMENTE criado — campo 'nome' lido corretamente");

        // Bug #3+#4 corrigidos: agendamento deve ser criado (não mais rejeitado por Guid.Empty ou campos ausentes)
        _agendamentoRepo.Verify(
            r => r.Salvar(It.IsAny<Agendamento>()),
            Times.Once,
            "CA105/Bug#3+#4: Agendamento histórico deve ter sido REALMENTE criado via CriarHistorico");

        // ── 9. Verificar relatório N criados ≠ 0 ────────────────────────────────

        var relatorio = await _registroRepo.Object.ObterRelatorio(JobId);

        Assert.That(relatorio.TotalCriados, Is.EqualTo(2),
            "CA105: paciente + agendamento ambos criados → TotalCriados = 2");
        Assert.That(relatorio.PorEntidade.ContainsKey("paciente"), Is.True);
        Assert.That(relatorio.PorEntidade["paciente"].Criados, Is.EqualTo(1));
        Assert.That(relatorio.PorEntidade.ContainsKey("agendamento"), Is.True);
        Assert.That(relatorio.PorEntidade["agendamento"].Criados, Is.EqualTo(1));

        // ── 10. Verificar status final do job ────────────────────────────────────
        Assert.That(job.Status, Is.EqualTo(MigracaoJob.StatusConcluido),
            "CA105: todos os registros criados → job deve concluir sem erros");
    }

    // ─── CA118 — regressão: materialização não quebra o fluxo 001–006 ────────────

    [Test]
    public async Task CA118_MaterializacaoNaoQuebra_FluxoAddendum001A006()
    {
        // Verifica que um job sem arquivo (ArquivoS3Key vazio) materializa zero registros
        // sem lançar exceção — fluxo continua para a carga encontrar lista vazia.
        var job = MigracaoJob.Criar(EstabelecimentoId, Guid.NewGuid());
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(job, JobId);
        // Registra com chave válida para passar a validação do domínio,
        // depois força ArquivoS3Key para null via reflection (simula job sem arquivo).
        job.RegistrarArquivoRecebido("migracao/42/200/placeholder.zip");
        typeof(MigracaoJob).GetProperty("ArquivoS3Key")!.SetValue(job, null);
        // Força para mapa_em_revisao ignorando as transições intermediárias
        typeof(MigracaoJob).GetProperty("Status")!.SetValue(job, MigracaoJob.StatusMapaEmRevisao);

        _jobRepo.Setup(r => r.ObterPorIdAdminOuNulo(JobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _mapaRepo.Setup(r => r.ListarPorJob(JobId, EstabelecimentoId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<MigracaoMapa>());

        var sut = new MaterializarRegistrosCommandHandler(
            _jobRepo.Object, _mapaRepo.Object, _registroRepo.Object,
            _storage.Object, new[] { _parser.Object },
            NullLogger<MaterializarRegistrosCommandHandler>.Instance);

        // Não deve lançar
        Assert.DoesNotThrowAsync(() => sut.ExecutarAsync(JobId));

        // Sem registros → lista vazia
        Assert.That(_registrosEmMemoria.Count, Is.EqualTo(0));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private CarregarOnda1JobHandler CriarCarregarHandler() => new(
        _jobRepo.Object,
        _registroRepo.Object,
        _eventoRepo.Object,
        _pacienteRepo.Object,
        _categoriaRepo.Object,
        _fabricanteRepo.Object,
        _fornecedorRepo.Object,
        _localRepo.Object,
        _itemRepo.Object,
        _agendamentoRepo.Object,
        _cirurgiaRepo.Object,
        _produtoRepo.Object,
        _cirurgiaLookup.Object,
        _produtoLookup.Object,
        _pacienteLookup.Object,
        _agendamentoLookup.Object,
        NullLogger<CarregarOnda1JobHandler>.Instance);

    private static Stream CriarZipComEntrada(string nome, byte[] conteudo)
    {
        var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(nome);
            using var entryStream = entry.Open();
            entryStream.Write(conteudo);
        }
        ms.Position = 0;
        return ms;
    }
}
