using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imedto.Backend.API.Filters;
using Imedto.Backend.Contracts.Pacientes.Commands;
using System.Text.Json.Serialization;
using Imedto.Backend.Contracts.Pacientes.Queries;
using Imedto.Backend.Contracts.Pacientes.Queries.Results;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.API.Controllers;

/// <summary>
/// Gerenciamento de pacientes do estabelecimento ativo. Todos os endpoints exigem o
/// header <c>X-Estabelecimento-Id</c> via <see cref="RequiresEstabelecimentoAttribute"/>.
/// </summary>
[Authorize]
[RequiresEstabelecimento]
[RequiresAssinaturaAtiva]
[RequiresAcao("pacientes")]
[ApiController]
[Route("api/paciente")]
[Produces("application/json")]
public class PacienteController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    private readonly IRequestBus _requestBus;
    private readonly ICurrentTenantAccessor _tenant;

    public PacienteController(
        ICommandBus commandBus,
        IRequestBus requestBus,
        ICurrentTenantAccessor tenant)
    {
        _commandBus = commandBus;
        _requestBus = requestBus;
        _tenant = tenant;
    }

    /// <summary>Lista paginada de pacientes do estabelecimento atual, com busca por nome ou CPF.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaPacientesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string busca = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10)
    {
        var pagina1 = await _requestBus.Query<ListarPacientesQuery, PaginaPacientesDto>(
            new ListarPacientesQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Busca = busca,
                Pagina = pagina,
                TamanhoPagina = tamanho
            });

        return Ok(pagina1);
    }

    /// <summary>
    /// Autocomplete de paciente (LGPD: apenas <c>id</c> + <c>nomeCompleto</c>).
    /// Usado em seletores que NÃO exibem CPF/telefone/data nascimento (ex: novo
    /// agendamento). Para a lista completa de pacientes, use <see cref="Listar"/>.
    /// </summary>
    [HttpGet("busca-rapida")]
    [ProducesResponseType(typeof(IReadOnlyList<PacienteBuscaRapidaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BuscaRapida(
        [FromQuery] string q = null,
        [FromQuery] int limite = 10)
    {
        var dados = await _requestBus.Query<BuscaRapidaPacientesQuery, IReadOnlyList<PacienteBuscaRapidaDto>>(
            new BuscaRapidaPacientesQuery
            {
                EstabelecimentoId = _tenant.EstabelecimentoId,
                Q = q,
                Limite = limite
            });
        return Ok(dados);
    }

    /// <summary>KPIs agregados (total + novos no mês) — usados no header da lista.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(PacienteStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Stats()
    {
        var dto = await _requestBus.Query<ObterPacienteStatsQuery, PacienteStatsDto>(
            new ObterPacienteStatsQuery { EstabelecimentoId = _tenant.EstabelecimentoId });
        return Ok(dto);
    }

    /// <summary>
    /// Retorna os dados de um paciente.
    /// Aceite opcional <c>?contato=mascarado</c>: CPF e telefone retornam ofuscados
    /// (ex.: "•••.•••.•••-09"), adequado para o app mobile (minimização LGPD).
    /// Sem o parâmetro (default) — comportamento inalterado: PII completa (web).
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(PacienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(long id, [FromQuery] string contato = null)
    {
        var dto = await _requestBus.Query<ObterPacienteQuery, PacienteDto>(
            new ObterPacienteQuery
            {
                PacienteId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                MascararContato = string.Equals(contato, "mascarado", StringComparison.OrdinalIgnoreCase)
            });

        if (dto is null) return NotFound();
        return Ok(dto);
    }

    /// <summary>Cadastra um novo paciente no estabelecimento atual. Apenas Profissional ou Dono.</summary>
    [HttpPost]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] PacienteRequest request)
    {
        await _commandBus.Send(new CadastrarPacienteCommand
        {
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            NomeCompleto = request.NomeCompleto,
            Cpf = request.Cpf,
            DocumentoInternacional = request.DocumentoInternacional,
            DataNascimento = request.DataNascimento,
            Genero = request.Genero,
            Telefone = request.Telefone,
            Email = request.Email,
            Endereco = request.Endereco,
            Observacoes = request.Observacoes,
            Tags = request.Tags ?? Array.Empty<string>(),
            Alertas = Array.Empty<string>(), // Alertas não são definidos no cadastro administrativo (LGPD 2026-06-22_002)
            WhatsappLembreteOptIn = request.WhatsappLembreteOptIn,
        });

        return Created(string.Empty, null);
    }

    /// <summary>Atualiza os dados de um paciente. Apenas Profissional ou Dono (Observacoes pode ter dado clinico).</summary>
    [HttpPut("{id:long}")]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(long id, [FromBody] PacienteRequest request)
    {
        await _commandBus.Send(new AtualizarPacienteCommand
        {
            PacienteId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            NomeCompleto = request.NomeCompleto,
            Cpf = request.Cpf,
            DocumentoInternacional = request.DocumentoInternacional,
            DataNascimento = request.DataNascimento,
            Genero = request.Genero,
            Telefone = request.Telefone,
            Email = request.Email,
            Endereco = request.Endereco,
            Observacoes = request.Observacoes,
            Tags = request.Tags ?? Array.Empty<string>(),
            // Alertas não atualizados pelo caminho administrativo (LGPD 2026-06-22_002):
            // o handler ignorará null e preservará o valor existente do aggregate.
            Alertas = null,
            WhatsappLembreteOptIn = request.WhatsappLembreteOptIn,
        });

        return NoContent();
    }

    /// <summary>
    /// Atualização parcial dos dados básicos de identificação — uso do app mobile (edição rápida).
    /// Campos omitidos ou null no JSON são ignorados (valor atual preservado). Preserva: genero,
    /// endereco, observacoes, tags, alertas, consentimento WhatsApp, documentoInternacional.
    /// Para atualização completa (web), usar <c>PUT /api/paciente/{id}</c>.
    /// </summary>
    [HttpPatch("{id:long}/dados-basicos")]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AtualizarDadosBasicos(long id, [FromBody] PacienteDadosBasicosRequest request)
    {
        await _commandBus.Send(new AtualizarDadosBasicosPacienteCommand
        {
            PacienteId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId,
            NomeCompleto = request.NomeCompleto,
            Telefone = request.Telefone,
            Email = request.Email,
            DataNascimento = request.DataNascimento,
            DataNascimentoFoiEnviada = request.DataNascimentoFoiEnviada,
            Cpf = request.Cpf,
            CpfFoiEnviado = request.CpfFoiEnviado,
        });

        return NoContent();
    }

    /// <summary>Soft delete (LGPD). Mantém registro marcado por retenção legal. Apenas Profissional ou Dono.</summary>
    [HttpDelete("{id:long}")]
    [RequiresPapel(TenantPapel.Profissional, TenantPapel.Dono)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deletar(long id)
    {
        await _commandBus.Send(new DeletarPacienteCommand
        {
            PacienteId = id,
            EstabelecimentoId = _tenant.EstabelecimentoId,
            SolicitanteUsuarioId = _tenant.UsuarioId
        });

        return NoContent();
    }

    /// <summary>
    /// Revelação auditada de CPF e telefone completos (LGPD — acesso explícito a PII sensível).
    /// Registra trilha de auditoria com motivo <c>RevelacaoDadosSensiveis</c>.
    /// Mesmo RBAC/tenant da ficha; 404 genérico se paciente não pertence ao estabelecimento.
    ///
    /// Minimização: o DTO de detalhe (<c>GET /api/paciente/{id}</c>) mantém esses campos
    /// por dependência do web (formulários de edição, PDFs, termos, agenda). Este endpoint
    /// serve consumidores que precisam do valor completo com trilha de auditoria explícita.
    /// </summary>
    [HttpGet("{id:long}/dados-sensiveis")]
    [ProducesResponseType(typeof(DadosSensiveisPacienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterDadosSensiveis(long id)
    {
        var dto = await _requestBus.Query<ObterDadosSensiveisPacienteQuery, DadosSensiveisPacienteDto>(
            new ObterDadosSensiveisPacienteQuery
            {
                PacienteId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId
            });

        if (dto is null) return NotFound();
        return Ok(dto);
    }

    /// <summary>LGPD Art. 18 — exporta todos os dados pessoais do paciente em JSON. Apenas Dono (acesso a TODA PII do titular).</summary>
    [HttpGet("{id:long}/exportar-dados")]
    [RequiresPapel(TenantPapel.Dono)]
    [ProducesResponseType(typeof(PacienteExportLgpdDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExportarDados(long id)
    {
        var dados = await _requestBus.Query<ExportarDadosPacienteQuery, PacienteExportLgpdDto>(
            new ExportarDadosPacienteQuery
            {
                PacienteId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId
            });

        return Ok(dados);
    }

    /// <summary>
    /// Relatório de acessos LGPD (Art. 9º/18) — lista paginada de quem acessou os dados
    /// do paciente, quando e o quê, em linguagem leiga. Apenas papel Dono.
    /// Audit: a própria consulta é auditada (Leitura) — R4/CA10.
    /// </summary>
    [HttpGet("{id:long}/acessos")]
    [RequiresPapel(TenantPapel.Dono)]
    [ProducesResponseType(typeof(Contracts.Pacientes.Queries.Results.PaginaAcessosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ListarAcessos(
        long id,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10)
    {
        var dto = await _requestBus.Query<Contracts.Pacientes.Queries.ListarAcessosDoPacienteQuery,
                                          Contracts.Pacientes.Queries.Results.PaginaAcessosDto>(
            new Contracts.Pacientes.Queries.ListarAcessosDoPacienteQuery
            {
                PacienteId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                Pagina = pagina,
                TamanhoPagina = tamanho,
            });
        return Ok(dto);
    }

    /// <summary>
    /// Listagem paginada e unificada dos documentos clínicos finalizados do paciente
    /// (receitas emitidas, atestados e pedidos de exame). Somente leitura.
    /// Audit LGPD: registra 1 acesso de leitura ao prontuário por carga.
    /// </summary>
    [HttpGet("{id:long}/documentos")]
    [ProducesResponseType(typeof(PaginaDocumentosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ListarDocumentos(
        long id,
        [FromQuery] string? tipo = null,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null,
        [FromQuery] string? busca = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 10)
    {
        var dto = await _requestBus.Query<ListarDocumentosDoPacienteQuery, PaginaDocumentosDto>(
            new ListarDocumentosDoPacienteQuery
            {
                PacienteId = id,
                EstabelecimentoId = _tenant.EstabelecimentoId,
                SolicitanteUsuarioId = _tenant.UsuarioId,
                Tipo = tipo,
                DataInicio = dataInicio,
                DataFim = dataFim,
                Busca = busca,
                Pagina = pagina,
                TamanhoPagina = tamanho,
            });
        return Ok(dto);
    }
}

public record PacienteRequest(
    string NomeCompleto,
    string Cpf,
    string DocumentoInternacional,
    [property: System.Text.Json.Serialization.JsonConverter(typeof(Imedto.Backend.SharedKernel.Json.DateOnlyAsYmdJsonConverter))]
    DateTime? DataNascimento,
    string Genero,
    string Telefone,
    string Email,
    string Endereco,
    string Observacoes,
    IReadOnlyList<string> Tags = null,
    // Alertas removidos do payload administrativo (LGPD briefing 2026-06-22_002).
    // A gestão de alertas é feita exclusivamente via PUT /api/paciente/{id}/prontuario/alertas,
    // gated por papel/vínculo de atendimento.
    /// <summary>
    /// Consentimento explícito do paciente para receber lembretes via WhatsApp (LGPD — R4).
    /// Null = não alterar (em PUT) ou false (em POST).
    /// </summary>
    bool? WhatsappLembreteOptIn = null);

/// <summary>
/// Payload de atualização parcial para o app mobile (PATCH /api/paciente/{id}/dados-basicos).
/// Campos omitidos no JSON são null → valor atual do paciente é preservado.
/// Para distinguir "dataNascimento não enviada" de "dataNascimento enviada como null" (limpar),
/// use a flag <see cref="DataNascimentoFoiEnviada"/>. Idem para CPF via <see cref="CpfFoiEnviado"/>.
/// </summary>
public record PacienteDadosBasicosRequest(
    string NomeCompleto = null,
    string Telefone = null,
    string Email = null,
    [property: JsonConverter(typeof(Imedto.Backend.SharedKernel.Json.DateOnlyAsYmdJsonConverter))]
    DateTime? DataNascimento = null,
    bool DataNascimentoFoiEnviada = false,
    string Cpf = null,
    bool CpfFoiEnviado = false);
