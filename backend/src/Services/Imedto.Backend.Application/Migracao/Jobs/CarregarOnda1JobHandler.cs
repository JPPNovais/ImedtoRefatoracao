using System.Text.Json;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Inventario.Cadastros;
using Imedto.Backend.Domain.Jobs;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Domain;
using Microsoft.Extensions.Logging;

namespace Imedto.Backend.Application.Migracao.Jobs;

public sealed class CarregarOnda1JobHandler : IJobHandler
{
    public string Nome => "carregar-onda1-migracao";

    private const int TamanhoLote = 100;

    // Ordem de processamento: respeita FKs
    // Cadastros-base primeiro, depois itens que dependem deles, depois pacientes, depois agenda.
    private static readonly string[] OrdemEntidades =
    [
        "fornecedor_estoque", "categoria_estoque", "fabricante_estoque", "local_estoque",
        "item_estoque",
        "produto_orcamento", "procedimento_orcamento", "paciente", "agendamento"
    ];

    private readonly IMigracaoJobRepository _jobRepo;
    private readonly IMigracaoRegistroRepository _registroRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly ICategoriaEstoqueRepository _categoriaRepo;
    private readonly IFabricanteEstoqueRepository _fabricanteRepo;
    private readonly IFornecedorEstoqueRepository _fornecedorRepo;
    private readonly ILocalEstoqueRepository _localRepo;
    private readonly IItemInventarioRepository _itemRepo;
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly ICatalogoCirurgiaRepository _cirurgiaRepo;
    private readonly ICatalogoProdutoRepository _produtoRepo;
    private readonly IMigracaoCatalogoCirurgiaLookup _cirurgiaLookup;
    private readonly IMigracaoCatalogoProdutoLookup _produtoLookup;
    private readonly ILogger<CarregarOnda1JobHandler> _logger;

    public CarregarOnda1JobHandler(
        IMigracaoJobRepository jobRepo,
        IMigracaoRegistroRepository registroRepo,
        IPacienteRepository pacienteRepo,
        ICategoriaEstoqueRepository categoriaRepo,
        IFabricanteEstoqueRepository fabricanteRepo,
        IFornecedorEstoqueRepository fornecedorRepo,
        ILocalEstoqueRepository localRepo,
        IItemInventarioRepository itemRepo,
        IAgendamentoRepository agendamentoRepo,
        ICatalogoCirurgiaRepository cirurgiaRepo,
        ICatalogoProdutoRepository produtoRepo,
        IMigracaoCatalogoCirurgiaLookup cirurgiaLookup,
        IMigracaoCatalogoProdutoLookup produtoLookup,
        ILogger<CarregarOnda1JobHandler> logger)
    {
        _jobRepo = jobRepo;
        _registroRepo = registroRepo;
        _pacienteRepo = pacienteRepo;
        _categoriaRepo = categoriaRepo;
        _fabricanteRepo = fabricanteRepo;
        _fornecedorRepo = fornecedorRepo;
        _localRepo = localRepo;
        _itemRepo = itemRepo;
        _agendamentoRepo = agendamentoRepo;
        _cirurgiaRepo = cirurgiaRepo;
        _produtoRepo = produtoRepo;
        _cirurgiaLookup = cirurgiaLookup;
        _produtoLookup = produtoLookup;
        _logger = logger;
    }

    public async Task ExecutarAsync(CancellationToken ct)
    {
        var job = await _jobRepo.ObterMaisAntigoMigrandoOuNulo(ct);
        if (job is null) return;

        _logger.LogInformation("[Job:{Nome}] Iniciando carga para job {JobId}.", Nome, job.Id);

        try
        {
            await ProcessarJobAsync(job, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Job:{Nome}] Falha inesperada no job {JobId}.", Nome, job.Id);
            throw;
        }
    }

    private async Task ProcessarJobAsync(MigracaoJob job, CancellationToken ct)
    {
        var todos = await _registroRepo.ListarPorJob(job.Id, ct);
        var pendentes = todos.Where(r => r.Status == "pendente").ToList();

        if (pendentes.Count == 0)
        {
            job.MarcarConcluido();
            await _jobRepo.Salvar(job, ct);
            return;
        }

        // Ordena por prioridade de entidade (FK-safe)
        var ordenados = pendentes
            .OrderBy(r =>
            {
                var idx = Array.IndexOf(OrdemEntidades, r.Entidade);
                return idx >= 0 ? idx : 999;
            })
            .ToList();

        var temRejeitados = false;

        // Lotes de 100
        for (var i = 0; i < ordenados.Count; i += TamanhoLote)
        {
            if (ct.IsCancellationRequested) break;
            var lote = ordenados.Skip(i).Take(TamanhoLote).ToList();
            foreach (var reg in lote)
            {
                await ProcessarRegistroAsync(job.EstabelecimentoId, reg, ct);
                if (reg.Status == "rejeitado") temRejeitados = true;
                await _registroRepo.Salvar(reg, ct);
            }
        }

        if (temRejeitados)
            job.MarcarConcluidoComErros();
        else
            job.MarcarConcluido();

        await _jobRepo.Salvar(job, ct);

        _logger.LogInformation("[Job:{Nome}] Job {JobId} concluído. Status: {Status}.", Nome, job.Id, job.Status);
    }

    private async Task ProcessarRegistroAsync(long estabelecimentoId, MigracaoRegistro reg, CancellationToken ct)
    {
        try
        {
            var payload = ParsePayload(reg.PayloadBruto);

            switch (reg.Entidade)
            {
                case "fornecedor_estoque":
                    await ProcessarFornecedorAsync(estabelecimentoId, reg, payload, ct);
                    break;
                case "categoria_estoque":
                    await ProcessarCategoriaAsync(estabelecimentoId, reg, payload, ct);
                    break;
                case "fabricante_estoque":
                    await ProcessarFabricanteAsync(estabelecimentoId, reg, payload, ct);
                    break;
                case "local_estoque":
                    await ProcessarLocalAsync(estabelecimentoId, reg, payload, ct);
                    break;
                case "item_estoque":
                    await ProcessarItemInventarioAsync(estabelecimentoId, reg, payload, ct);
                    break;
                case "produto_orcamento":
                    await ProcessarProdutoOrcamentoAsync(estabelecimentoId, reg, payload, ct);
                    break;
                case "procedimento_orcamento":
                    await ProcessarProcedimentoOrcamentoAsync(estabelecimentoId, reg, payload, ct);
                    break;
                case "paciente":
                    await ProcessarPacienteAsync(estabelecimentoId, reg, payload, ct);
                    break;
                case "agendamento":
                    await ProcessarAgendamentoAsync(estabelecimentoId, reg, payload, ct);
                    break;
                default:
                    reg.MarcarPulado("entidade não suportada nesta onda");
                    break;
            }
        }
        catch (BusinessException ex)
        {
            reg.MarcarRejeitado(ex.Message);
        }
        // Exception inesperada sobe — não capturar aqui
    }

    private static Dictionary<string, string> ParsePayload(string payloadBruto)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(payloadBruto)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static string? G(Dictionary<string, string> p, string key)
        => p.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

    // FORNECEDOR
    private async Task ProcessarFornecedorAsync(long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        var razaoSocial = G(payload, "razao_social");
        var cnpj = G(payload, "cnpj");

        if (razaoSocial == null)
        {
            reg.MarcarRejeitado("identificador ausente");
            return;
        }

        FornecedorEstoque? existente = null;
        if (cnpj != null)
            existente = await _fornecedorRepo.ObterPorCnpjOuNulo(cnpj, estId);
        existente ??= await _fornecedorRepo.ObterPorNomeOuNulo(razaoSocial, estId);

        if (existente is not null)
        {
            existente.Atualizar(razaoSocial, null, cnpj, G(payload, "contato_nome"), G(payload, "telefone"), G(payload, "email"), 0);
            await _fornecedorRepo.Salvar(existente);
            reg.MarcarImportadoAtualizado(existente.Id);
        }
        else
        {
            var novo = FornecedorEstoque.Criar(estId, razaoSocial, null, cnpj, G(payload, "contato_nome"), G(payload, "telefone"), G(payload, "email"), 0);
            await _fornecedorRepo.Salvar(novo);
            reg.MarcarImportadoCriado(novo.Id);
        }
    }

    // CATEGORIA
    private async Task ProcessarCategoriaAsync(long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        var nome = G(payload, "nome");
        if (nome == null) { reg.MarcarRejeitado("identificador ausente"); return; }

        var existente = await _categoriaRepo.ObterPorNomeOuNulo(nome, estId);
        if (existente is not null)
        {
            reg.MarcarImportadoAtualizado(existente.Id);
        }
        else
        {
            var cor = G(payload, "cor") ?? "hsl(218 70% 50%)";
            var icone = G(payload, "icone") ?? "fa-box";
            var novo = CategoriaEstoque.Criar(estId, nome, cor, icone);
            await _categoriaRepo.Salvar(novo);
            reg.MarcarImportadoCriado(novo.Id);
        }
    }

    // FABRICANTE
    private async Task ProcessarFabricanteAsync(long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        var nome = G(payload, "nome");
        if (nome == null) { reg.MarcarRejeitado("identificador ausente"); return; }

        var existente = await _fabricanteRepo.ObterPorNomeOuNulo(nome, estId);
        if (existente is not null)
        {
            reg.MarcarImportadoAtualizado(existente.Id);
        }
        else
        {
            var novo = FabricanteEstoque.Criar(estId, nome, G(payload, "pais"));
            await _fabricanteRepo.Salvar(novo);
            reg.MarcarImportadoCriado(novo.Id);
        }
    }

    // LOCAL
    private async Task ProcessarLocalAsync(long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        var nome = G(payload, "nome");
        if (nome == null) { reg.MarcarRejeitado("identificador ausente"); return; }

        var existente = await _localRepo.ObterPorNomeOuNulo(nome, estId);
        if (existente is not null)
        {
            reg.MarcarImportadoAtualizado(existente.Id);
        }
        else
        {
            if (!Enum.TryParse<TipoLocalEstoque>(G(payload, "tipo") ?? "Armario", ignoreCase: true, out var tipo))
                tipo = TipoLocalEstoque.Armario;
            var novo = LocalEstoque.Criar(estId, nome, tipo, G(payload, "andar_setor"), G(payload, "responsavel"));
            await _localRepo.Salvar(novo);
            reg.MarcarImportadoCriado(novo.Id);
        }
    }

    // ITEM DE ESTOQUE
    private async Task ProcessarItemInventarioAsync(long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        var nome = G(payload, "nome");
        var codigo = G(payload, "codigo");

        if (nome == null) { reg.MarcarRejeitado("identificador ausente"); return; }

        // Resolve FK obrigatória: categoria (obrigatória — CA11)
        var categoriaNome = G(payload, "categoria_nome");
        if (categoriaNome == null) { reg.MarcarRejeitado("categoria obrigatória ausente"); return; }
        var categoria = await _categoriaRepo.ObterPorNomeOuNulo(categoriaNome, estId);
        if (categoria == null) { reg.MarcarRejeitado("categoria não encontrada"); return; }

        // FK opcionais: fornecedor, fabricante, local (CA11 — cria sem eles se ausentes)
        var fornecedorNome = G(payload, "fornecedor_nome");
        long? fornecedorId = null;
        if (fornecedorNome != null)
        {
            var fornecedor = await _fornecedorRepo.ObterPorNomeOuNulo(fornecedorNome, estId);
            fornecedorId = fornecedor?.Id;
        }

        var fabricanteNome = G(payload, "fabricante_nome");
        long? fabricanteId = null;
        if (fabricanteNome != null)
        {
            var fabricante = await _fabricanteRepo.ObterPorNomeOuNulo(fabricanteNome, estId);
            fabricanteId = fabricante?.Id;
        }

        var localNome = G(payload, "local_nome");
        long? localId = null;
        if (localNome != null)
        {
            var local = await _localRepo.ObterPorNomeOuNulo(localNome, estId);
            localId = local?.Id;
        }

        var unidade = G(payload, "unidade_medida") ?? "un";
        decimal.TryParse(G(payload, "quantidade_minima"), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var qMin);
        var custoUnitario = ParseDecimal(G(payload, "custo_unitario"));

        // Lookup de existência: código primeiro, fallback por nome
        ItemInventario? existente = null;
        if (codigo != null)
        {
            existente = await _itemRepo.ObterPorCodigoOuNulo(codigo, estId);
        }
        else
        {
            // Sem código: busca por nome. Se não encontrar, tenta criar com código = nome normalizado.
            // Se houver outro item com o mesmo nome E sem código identificador → sem chave única (CA8).
            var porNome = await _itemRepo.ObterPorNomeOuNulo(nome, estId);
            if (porNome is not null && porNome.Codigo != nome.Trim().ToUpperInvariant())
            {
                // Existe um item com esse nome mas com código diferente e nenhum código foi fornecido
                // → não há como garantir unicidade sem o código (CA8)
                reg.MarcarRejeitado("sem chave única para dedupe");
                return;
            }
            existente = porNome;
            // Sem código no arquivo → usa o nome normalizado como código provisório
            codigo = nome.Trim().ToUpperInvariant();
        }

        if (existente is not null)
        {
            existente.Atualizar(nome, categoria.Id, categoria.Nome, unidade, qMin, fabricanteId, fornecedorId, localId, custoUnitario);
            await _itemRepo.Salvar(existente);
            reg.MarcarImportadoAtualizado(existente.Id);
        }
        else
        {
            // BusinessException capturada pelo caller se código duplicado já foi inserido (CA8 via domínio)
            var novo = ItemInventario.Criar(estId, codigo!, nome, categoria.Id, categoria.Nome, unidade, qMin, fabricanteId, fornecedorId, localId, custoUnitario);
            await _itemRepo.Salvar(novo);
            reg.MarcarImportadoCriado(novo.Id);
        }
    }

    // PRODUTO ORÇAMENTO
    private async Task ProcessarProdutoOrcamentoAsync(long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        var nome = G(payload, "nome");
        var sku = G(payload, "codigo_sku");

        if (nome == null) { reg.MarcarRejeitado("identificador ausente"); return; }

        long? idExistente = null;
        if (sku != null)
            idExistente = await _produtoLookup.ObterIdPorCodigoOuNulo(sku, estId, ct);
        idExistente ??= await _produtoLookup.ObterIdPorNomeOuNulo(nome, estId, ct);

        if (idExistente.HasValue)
        {
            var existente = await _produtoRepo.ObterPorIdOuNulo(idExistente.Value, estId);
            if (existente is not null)
            {
                existente.Atualizar(nome, G(payload, "descricao"), ParseDecimal(G(payload, "valor_referencia")), false, codigoSku: sku);
                await _produtoRepo.Salvar(existente);
                reg.MarcarImportadoAtualizado(existente.Id);
            }
        }
        else
        {
            var novo = CatalogoProduto.Criar(estId, nome, G(payload, "descricao"), ParseDecimal(G(payload, "valor_referencia")), false, codigoSku: sku);
            await _produtoRepo.Salvar(novo);
            reg.MarcarImportadoCriado(novo.Id);
        }
    }

    // PROCEDIMENTO ORÇAMENTO
    private async Task ProcessarProcedimentoOrcamentoAsync(long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        var descricao = G(payload, "descricao");
        var codigo = G(payload, "codigo_interno");

        if (descricao == null) { reg.MarcarRejeitado("identificador ausente"); return; }

        long? idExistente = null;
        if (codigo != null)
            idExistente = await _cirurgiaLookup.ObterIdPorCodigoOuNulo(codigo, estId, ct);
        idExistente ??= await _cirurgiaLookup.ObterIdPorNomeOuNulo(descricao, estId, ct);

        if (idExistente.HasValue)
        {
            var existente = await _cirurgiaRepo.ObterPorIdOuNulo(idExistente.Value, estId);
            if (existente is not null)
            {
                existente.Atualizar(descricao, ParseDecimal(G(payload, "valor_base")) ?? 0m, null, codigo);
                await _cirurgiaRepo.Salvar(existente);
                reg.MarcarImportadoAtualizado(existente.Id);
            }
        }
        else
        {
            var novo = CatalogoCirurgia.Criar(estId, descricao, ParseDecimal(G(payload, "valor_base")) ?? 0m, null, codigo);
            await _cirurgiaRepo.Salvar(novo);
            reg.MarcarImportadoCriado(novo.Id);
        }
    }

    // PACIENTE
    private async Task ProcessarPacienteAsync(long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        var nome = G(payload, "nome_completo");
        var cpf = G(payload, "cpf");
        var docInt = G(payload, "documento_internacional");
        var telefone = G(payload, "telefone");

        Paciente? existente = null;
        if (cpf != null)
            existente = await _pacienteRepo.ObterPorCpfOuNulo(cpf, estId);
        else if (docInt != null)
            existente = await _pacienteRepo.ObterPorDocumentoInternacionalOuNulo(docInt, estId);
        else if (nome != null && telefone != null)
            existente = await _pacienteRepo.ObterPorNomeTelefoneOuNulo(nome, telefone, estId);
        else
        {
            reg.MarcarPulado("identificador ausente");
            return;
        }

        if (nome == null) { reg.MarcarRejeitado("nome obrigatório"); return; }

        if (!Enum.TryParse<GeneroPaciente>(G(payload, "genero"), ignoreCase: true, out var genero))
            genero = GeneroPaciente.NaoInformado;
        DateTime? dataNasc = null;
        if (G(payload, "data_nascimento") is { } dnStr && DateTime.TryParse(dnStr, out var dn))
            dataNasc = dn;

        if (existente is not null)
        {
            existente.AtualizarDados(nome, cpf, dataNasc, genero, telefone, G(payload, "email"), G(payload, "endereco"), G(payload, "observacoes"), docInt);
            await _pacienteRepo.Salvar(existente);
            reg.MarcarImportadoAtualizado(existente.Id);
        }
        else
        {
            var novo = Paciente.Cadastrar(estId, nome, cpf, dataNasc, genero, telefone, G(payload, "email"), G(payload, "endereco"), G(payload, "observacoes"), docInt);
            await _pacienteRepo.Salvar(novo);
            reg.MarcarImportadoCriado(novo.Id);
        }
    }

    // AGENDAMENTO
    private async Task ProcessarAgendamentoAsync(long estId, MigracaoRegistro reg, Dictionary<string, string> payload, CancellationToken ct)
    {
        if (!long.TryParse(G(payload, "paciente_id"), out var pacienteId) ||
            !Guid.TryParse(G(payload, "profissional_usuario_id"), out var profId) ||
            !DateTime.TryParse(G(payload, "inicio_previsto"), out var inicio))
        {
            reg.MarcarPulado("dados de agendamento insuficientes");
            return;
        }

        var existente = await _agendamentoRepo.ObterPorChaveDeNegocioOuNulo(pacienteId, profId, inicio, estId);
        if (existente is not null)
        {
            reg.MarcarPulado("agendamento já existe");
            return;
        }

        var fimPrevisto = DateTime.TryParse(G(payload, "fim_previsto"), out var fim) ? fim : inicio.AddHours(1);
        var tipoServico = G(payload, "tipo_servico") ?? "Consulta";
        // Agendamento.Criar valida que inicioPrevisto >= UtcNow - 5min — agendamentos históricos
        // serão rejeitados com BusinessException → marcado como rejeitado automaticamente.
        var novo = Agendamento.Criar(estId, pacienteId, profId, Guid.Empty, inicio, fimPrevisto, tipoServico, G(payload, "observacoes"));
        await _agendamentoRepo.Salvar(novo);
        reg.MarcarImportadoCriado(novo.Id);
    }

    private static decimal? ParseDecimal(string? s) =>
        s != null && decimal.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
}
