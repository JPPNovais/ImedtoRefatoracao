using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

public sealed class AtualizarModeloTermoCommandHandler : ICommandHandler<AtualizarModeloTermoCommand>
{
    private readonly ITermoModeloRepository _repo;
    private readonly ITermoHtmlSanitizer _sanitizer;
    private readonly ITermoAuditLogger _audit;

    public AtualizarModeloTermoCommandHandler(
        ITermoModeloRepository repo,
        ITermoHtmlSanitizer sanitizer,
        ITermoAuditLogger audit)
    {
        _repo = repo;
        _sanitizer = sanitizer;
        _audit = audit;
    }

    public async Task Handle(AtualizarModeloTermoCommand cmd)
    {
        var modelo = await _repo.ObterPorIdDoEstabelecimentoOuNulo(cmd.ModeloId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Modelo não encontrado.");

        var categoria = TermoParsers.ParseCategoria(cmd.Categoria);
        var conteudoSanitizado = _sanitizer.Sanitizar(cmd.ConteudoHtml);
        if (string.IsNullOrWhiteSpace(conteudoSanitizado))
            throw new BusinessException("Conteúdo é obrigatório.");

        var versaoBumpada = modelo.Atualizar(categoria, cmd.Titulo, conteudoSanitizado);
        await _repo.Salvar(modelo);

        if (versaoBumpada)
        {
            var versao = modelo.CriarSnapshotVersaoAtual(cmd.SolicitanteUsuarioId);
            await _repo.SalvarVersao(versao);
        }

        await _audit.RegistrarAsync(
            cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId,
            "modelo-editado", "TermoModelo", modelo.Id,
            metadataJson: $"{{\"versao_apos\":{modelo.VersaoAtual},\"conteudo_mudou\":{versaoBumpada.ToString().ToLowerInvariant()}}}");
    }
}
