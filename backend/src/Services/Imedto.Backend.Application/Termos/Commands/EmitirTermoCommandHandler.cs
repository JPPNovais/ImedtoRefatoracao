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
/// 2. Carrega o modelo — aceita modelo do estabelecimento OU padrão do sistema
///    (paciente pode receber direto um padrão sem precisar clonar antes).
/// 3. Resolve as variáveis no contexto do paciente/estabelecimento/profissional.
/// 4. Sanitiza o HTML resolvido (paranóia — variável poderia inserir tag arbitrária).
/// 5. Cria o aggregate <see cref="TermoEmitido"/> com snapshot + hash + token.
/// 6. Persiste + dispara <see cref="Domain.Termos.Events.TermoEmitidoEvent"/>.
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

    /// <summary>TTL default do link público de aceite (7 dias).</summary>
    public static readonly TimeSpan TtlLinkPublico = TimeSpan.FromDays(7);

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

        var assinaturaTipo = TermoParsers.ParseAssinaturaTipo(cmd.AssinaturaTipo);

        // Defense-in-depth UX: se aceite_link + canal email, paciente precisa ter e-mail.
        // O front também trava isso, mas regra de negócio é fonte da verdade (CLAUDE.md §1).
        if (assinaturaTipo == AssinaturaTipo.AceiteLink
            && string.Equals(cmd.CanalEnvio, "email", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(paciente.Email))
        {
            throw new BusinessException("Paciente sem e-mail cadastrado. Use 'copiar link' ou cadastre o e-mail antes.");
        }

        // 2. Modelo do tenant OU padrão do sistema.
        var modelo = await _modeloRepo.ObterPorIdDoEstabelecimentoOuNulo(cmd.ModeloId, cmd.EstabelecimentoId);
        modelo ??= await _modeloRepo.ObterPadraoDoSistemaPorIdOuNulo(cmd.ModeloId);
        if (modelo is null)
            throw new BusinessException("Modelo de termo não encontrado.");
        if (!modelo.Ativo)
            throw new BusinessException("Modelo de termo inativo.");

        // 3. Resolve variáveis com contexto do tenant.
        // O emissor (recepção/Dono) não é tratado como profissional automaticamente.
        // Variáveis {{profissional.*}} só são preenchidas quando ProfissionalUsuarioId
        // explícito é informado e válido — caso contrário, caem em fallback no resolver.
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

        // 4. Re-sanitiza após resolver — variável pode ter contido HTML, e nunca
        //    confiamos no que sai do resolver para ir cru pro snapshot.
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
            assinaturaTipo,
            cmd.EmissorUsuarioId,
            TtlLinkPublico);

        await _termoRepo.Salvar(termo);
        termo.MarcarComoEmitido(cmd.CanalEnvio);
        cmd.TermoEmitidoId = termo.Id;
        cmd.TokenAceiteGerado = termo.TokenAceite;

        // 6. Eventos de domínio (despachados manualmente para Atestados-style; o
        //    dispatcher do UoW também coletaria — mantemos o padrão local pra
        //    explicitude e por permitir que o handler aborte se um listener falhar).
        foreach (var ev in termo.DomainEvents)
            await _eventBus.Publish(ev);
        termo.ClearDomainEvents();

        await _audit.RegistrarAsync(
            cmd.EstabelecimentoId, cmd.EmissorUsuarioId,
            "termo-emitido", "TermoEmitido", termo.Id,
            metadataJson: $"{{\"paciente_id\":{paciente.Id},\"modelo_id\":{modelo.Id},\"assinatura_tipo\":\"{TermoParsers.SerializarAssinaturaTipo(assinaturaTipo)}\"}}");
    }
}
