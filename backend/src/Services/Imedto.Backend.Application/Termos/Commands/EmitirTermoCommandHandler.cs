using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Domain.Vinculos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

/// <summary>
/// Emite um novo termo para um paciente. Fluxo:
/// 1. Valida paciente do tenant (multi-tenant defense-in-depth).
/// 2. Carrega o modelo — aceita modelo do estabelecimento OU padrão do sistema.
/// 3. Resolve as variáveis no contexto do paciente/estabelecimento/profissional.
/// 4. Sanitiza o HTML resolvido (paranóia — variável poderia inserir tag arbitrária).
/// 5. Cria o aggregate <see cref="TermoEmitido"/> com snapshot + hash.
/// 6. Persiste + dispara <see cref="Domain.Termos.Events.TermoEmitidoEvent"/>.
///
/// O aceite por link público foi removido (briefing 2026-06-12_002). O único fluxo
/// de assinatura é o documento físico (<see cref="AssinaturaTipo.PdfAnexado"/>).
/// </summary>
public sealed class EmitirTermoCommandHandler : ICommandHandler<EmitirTermoCommand>
{
    private readonly ITermoEmitidoRepository _termoRepo;
    private readonly ITermoModeloRepository _modeloRepo;
    private readonly IPacienteRepository _pacienteRepo;
    private readonly IVinculoRepository _vinculoRepo;
    private readonly ITermoResolverDeVariaveis _resolver;
    private readonly ITermoHtmlSanitizer _sanitizer;
    private readonly ITermoTextoExtractor _textoExtractor;
    private readonly ITermoAuditLogger _audit;
    private readonly IEventBus _eventBus;

    public EmitirTermoCommandHandler(
        ITermoEmitidoRepository termoRepo,
        ITermoModeloRepository modeloRepo,
        IPacienteRepository pacienteRepo,
        IVinculoRepository vinculoRepo,
        ITermoResolverDeVariaveis resolver,
        ITermoHtmlSanitizer sanitizer,
        ITermoTextoExtractor textoExtractor,
        ITermoAuditLogger audit,
        IEventBus eventBus)
    {
        _termoRepo = termoRepo;
        _modeloRepo = modeloRepo;
        _pacienteRepo = pacienteRepo;
        _vinculoRepo = vinculoRepo;
        _resolver = resolver;
        _sanitizer = sanitizer;
        _textoExtractor = textoExtractor;
        _audit = audit;
        _eventBus = eventBus;
    }

    public async Task Handle(EmitirTermoCommand cmd)
    {
        // 1. Paciente do tenant.
        var paciente = await _pacienteRepo.ObterPorIdOuNulo(cmd.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");
        if (paciente.EstaDeletado)
            throw new BusinessException("Paciente deletado — não é possível emitir termo.");

        // 2. Modelo do tenant OU padrão do sistema.
        var modelo = await _modeloRepo.ObterPorIdDoEstabelecimentoOuNulo(cmd.ModeloId, cmd.EstabelecimentoId);
        modelo ??= await _modeloRepo.ObterPadraoDoSistemaPorIdOuNulo(cmd.ModeloId);
        if (modelo is null)
            throw new BusinessException("Modelo de termo não encontrado.");
        if (!modelo.Ativo)
            throw new BusinessException("Modelo de termo inativo.");

        // 3. Resolve variáveis com contexto do tenant.
        Guid? profissionalUsuarioId = null;
        if (cmd.ProfissionalUsuarioId is { } pid && pid != Guid.Empty)
        {
            var podeAtuar = await _vinculoRepo.PodeAtuarComoProfissional(pid, cmd.EstabelecimentoId);
            if (!podeAtuar)
                throw new BusinessException("Profissional inválido.");
            profissionalUsuarioId = pid;
        }

        var contexto = new ContextoDeVariaveis(paciente.Id, cmd.EstabelecimentoId, profissionalUsuarioId);
        var conteudoResolvido = await _resolver.ResolverAsync(modelo.ConteudoHtml, contexto);

        // 4. Re-sanitiza após resolver.
        var snapshotHtml = _sanitizer.Sanitizar(conteudoResolvido);
        var snapshotTexto = _textoExtractor.Extrair(snapshotHtml);
        if (string.IsNullOrWhiteSpace(snapshotTexto))
            snapshotTexto = "(termo sem conteúdo textual)";

        // 5. Cria + persiste.
        var termo = TermoEmitido.Emitir(
            paciente.Id,
            cmd.EstabelecimentoId,
            modelo.Id,
            modelo.VersaoAtual,
            snapshotHtml,
            snapshotTexto,
            cmd.EmissorUsuarioId,
            cmd.EvolucaoId);

        await _termoRepo.Salvar(termo);
        termo.MarcarComoEmitido();
        cmd.TermoEmitidoId = termo.Id;

        // 6. Eventos de domínio.
        foreach (var ev in termo.DomainEvents)
            await _eventBus.Publish(ev);
        termo.ClearDomainEvents();

        await _audit.RegistrarAsync(
            cmd.EstabelecimentoId, cmd.EmissorUsuarioId,
            "termo-emitido", "TermoEmitido", termo.Id,
            metadataJson: $"{{\"paciente_id\":{paciente.Id},\"modelo_id\":{modelo.Id}}}");
    }
}
