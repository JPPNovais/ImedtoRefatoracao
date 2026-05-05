using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Cirurgias.Commands;

public class AtualizarEquipeCommandHandler : ICommandHandler<AtualizarEquipeCommand>
{
    private readonly IProcedimentoCirurgicoRepository _repo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public AtualizarEquipeCommandHandler(
        IProcedimentoCirurgicoRepository repo,
        IProntuarioAcessoLogService acessoLog)
    {
        _repo = repo;
        _acessoLog = acessoLog;
    }

    public async Task Handle(AtualizarEquipeCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var procedimento = await _repo.ObterPorIdOuNulo(cmd.ProcedimentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Procedimento não encontrado.");

        var nova = cmd.Equipe.Select(m =>
            new ProcedimentoCirurgico.EquipeInicialPayload(
                m.ProfissionalUsuarioId,
                Enum.TryParse<PapelCirurgia>(m.Papel, ignoreCase: true, out var p)
                    ? p
                    : throw new BusinessException($"Papel '{m.Papel}' inválido na equipe cirúrgica.")));

        procedimento.SubstituirEquipe(nova);
        await _repo.Salvar(procedimento);

        if (cmd.SolicitanteUsuarioId != Guid.Empty)
        {
            await _acessoLog.RegistrarAsync(
                procedimento.ProntuarioId, cmd.SolicitanteUsuarioId, cmd.EstabelecimentoId,
                TipoAcessoProntuario.Escrita);
        }
    }
}
