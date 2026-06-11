using Imedto.Backend.Contracts.Prontuarios.Commands;
using Imedto.Backend.Domain.Prontuarios.Pendencias;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Prontuarios.Commands;

/// <summary>
/// Conclui manualmente uma pendência de atendimento pelo painel (R14/CA67).
/// Multi-tenant: filtro por estabelecimentoId em ObterPorId (R5/CA69).
/// Mensagem genérica em "não encontrado" — não vaza existência cross-tenant.
/// </summary>
public class ConcluirPendenciaManualCommandHandler : ICommandHandler<ConcluirPendenciaManualCommand>
{
    private readonly IPendenciaAtendimentoRepository _repo;

    public ConcluirPendenciaManualCommandHandler(IPendenciaAtendimentoRepository repo)
    {
        _repo = repo;
    }

    public async Task Handle(ConcluirPendenciaManualCommand command)
    {
        var pendencia = await _repo.ObterPorId(command.PendenciaId, command.EstabelecimentoId)
            ?? throw new BusinessException("Pendência não encontrada.");

        pendencia.ConcluirManualmente();
        // A pendência é uma entidade tracked pelo EF — a UoW persiste a alteração.
    }
}
