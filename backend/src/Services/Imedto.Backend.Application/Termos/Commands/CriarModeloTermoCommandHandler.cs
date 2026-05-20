using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

public sealed class CriarModeloTermoCommandHandler : ICommandHandler<CriarModeloTermoCommand>
{
    private readonly ITermoModeloRepository _repo;
    private readonly ITermoHtmlSanitizer _sanitizer;
    private readonly ITermoAuditLogger _audit;

    public CriarModeloTermoCommandHandler(
        ITermoModeloRepository repo,
        ITermoHtmlSanitizer sanitizer,
        ITermoAuditLogger audit)
    {
        _repo = repo;
        _sanitizer = sanitizer;
        _audit = audit;
    }

    public async Task Handle(CriarModeloTermoCommand cmd)
    {
        var categoria = TermoParsers.ParseCategoria(cmd.Categoria);
        var conteudoSanitizado = _sanitizer.Sanitizar(cmd.ConteudoHtml);
        if (string.IsNullOrWhiteSpace(conteudoSanitizado))
            throw new BusinessException("Conteúdo é obrigatório.");

        var modelo = TermoModelo.CriarDoEstabelecimento(
            cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId, categoria, cmd.Titulo, conteudoSanitizado);

        await _repo.Salvar(modelo);
        var versao1 = modelo.CriarSnapshotVersaoAtual(cmd.SolicitanteUsuarioId);
        await _repo.SalvarVersao(versao1);

        cmd.ModeloIdCriado = modelo.Id;

        await _audit.RegistrarAsync(
            cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId,
            "modelo-criado", "TermoModelo", modelo.Id,
            metadataJson: $"{{\"categoria\":\"{TermoParsers.SerializarCategoria(categoria)}\"}}");
    }
}
