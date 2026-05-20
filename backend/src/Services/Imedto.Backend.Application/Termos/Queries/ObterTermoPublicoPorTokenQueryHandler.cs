using Imedto.Backend.Contracts.Termos.Dtos;
using Imedto.Backend.Contracts.Termos.Queries;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Queries;

/// <summary>
/// Query do fluxo público de aceite (Fase 4). NÃO filtra por tenant — o token é
/// segredo de 256 bits. Erros (token inválido, expirado, termo já respondido)
/// retornam BusinessException com mensagem genérica — o controller traduz em 410.
///
/// LGPD: retorna apenas nome do estab + nome do profissional emissor + título e
/// snapshot do termo. Não retorna paciente_id, e-mail ou CPF.
/// </summary>
public sealed class ObterTermoPublicoPorTokenQueryHandler
    : IRequestHandler<ObterTermoPublicoPorTokenQuery, TermoPublicoDto>
{
    /// <summary>Mesma constante usada no handler de POST (idempotência da mensagem).</summary>
    public const string MensagemLinkInvalido = Commands.RegistrarRespostaPublicaTermoCommandHandler.MensagemLinkInvalido;

    private readonly ITermoEmitidoRepository _termoRepo;
    private readonly IEstabelecimentoRepository _estabRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ITermoModeloRepository _modeloRepo;

    public ObterTermoPublicoPorTokenQueryHandler(
        ITermoEmitidoRepository termoRepo,
        IEstabelecimentoRepository estabRepo,
        IUsuarioRepository usuarioRepo,
        ITermoModeloRepository modeloRepo)
    {
        _termoRepo = termoRepo;
        _estabRepo = estabRepo;
        _usuarioRepo = usuarioRepo;
        _modeloRepo = modeloRepo;
    }

    public async Task<TermoPublicoDto> Handle(ObterTermoPublicoPorTokenQuery q)
    {
        if (string.IsNullOrWhiteSpace(q.Token))
            throw new BusinessException(MensagemLinkInvalido);

        var termo = await _termoRepo.ObterPorTokenOuNulo(q.Token);
        if (termo is null)
        {
            // Não persistimos log — não há termo_id válido. Apenas devolve genérico.
            throw new BusinessException(MensagemLinkInvalido);
        }

        // Sempre auditar acesso (até tentativas inválidas em termo existente).
        var acao = (termo.Status != StatusTermoEmitido.Pendente
                    || termo.TokenExpiraEm is null
                    || termo.TokenExpiraEm < DateTime.UtcNow)
            ? "tentativa_invalida"
            : "visualizou_publico";
        await _termoRepo.SalvarAcessoLog(TermoEmitidoAcessoLog.Registrar(termo.Id, q.IpOrigem, q.UserAgent, acao));

        if (termo.Status != StatusTermoEmitido.Pendente)
            throw new BusinessException(MensagemLinkInvalido);

        if (termo.TokenExpiraEm is null || termo.TokenExpiraEm < DateTime.UtcNow)
            throw new BusinessException(MensagemLinkInvalido);

        var estab = await _estabRepo.ObterPorIdOuNulo(termo.EstabelecimentoId);
        var emissor = await _usuarioRepo.ObterPorIdOuNulo(termo.EmitidoPorUsuarioId);
        var modelo = await _modeloRepo.ObterPorIdDoEstabelecimentoOuNulo(termo.TermoModeloId, termo.EstabelecimentoId)
            ?? await _modeloRepo.ObterPadraoDoSistemaPorIdOuNulo(termo.TermoModeloId);

        return new TermoPublicoDto
        {
            TituloModelo = modelo?.Titulo ?? "Termo de consentimento",
            ConteudoSnapshotHtml = termo.ConteudoSnapshotHtml,
            EstabelecimentoNome = estab?.NomeFantasia ?? "Estabelecimento",
            ProfissionalEmissor = emissor?.NomeCompleto ?? "Profissional emissor",
            EmitidoEm = termo.CriadoEm,
        };
    }
}
