using Imedto.Backend.Contracts.Termos.Commands;
using Imedto.Backend.Domain.Termos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Termos.Commands;

public sealed class ClonarModeloTermoCommandHandler : ICommandHandler<ClonarModeloTermoCommand>
{
    private readonly ITermoModeloRepository _repo;
    private readonly ITermoAuditLogger _audit;

    public ClonarModeloTermoCommandHandler(ITermoModeloRepository repo, ITermoAuditLogger audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task Handle(ClonarModeloTermoCommand cmd)
    {
        var padrao = await _repo.ObterPadraoDoSistemaPorIdOuNulo(cmd.ModeloPadraoId)
            ?? throw new BusinessException("Modelo padrão não encontrado.");

        var clone = TermoModelo.ClonarDePadrao(padrao, cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId);
        await _repo.Salvar(clone);
        var versao1 = clone.CriarSnapshotVersaoAtual(cmd.SolicitanteUsuarioId);
        await _repo.SalvarVersao(versao1);

        cmd.ModeloIdClonado = clone.Id;

        await _audit.RegistrarAsync(
            cmd.EstabelecimentoId, cmd.SolicitanteUsuarioId,
            "modelo-clonado", "TermoModelo", clone.Id,
            metadataJson: $"{{\"padrao_id\":{padrao.Id}}}");
    }
}
