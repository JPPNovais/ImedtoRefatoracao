using Imedto.Backend.Contracts.Cirurgias.Commands;
using Imedto.Backend.Domain.Cirurgias;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using ContratoDroga = Imedto.Backend.Contracts.Cirurgias.DrogaAnestesica;
using ContratoMonitor = Imedto.Backend.Contracts.Cirurgias.MonitorizacaoVital;

namespace Imedto.Backend.Application.Cirurgias.Commands;

public class RegistrarRealizacaoCommandHandler : ICommandHandler<RegistrarRealizacaoCommand>
{
    private readonly IProcedimentoCirurgicoRepository _repo;
    private readonly IProntuarioAcessoLogService _acessoLog;

    public RegistrarRealizacaoCommandHandler(
        IProcedimentoCirurgicoRepository repo,
        IProntuarioAcessoLogService acessoLog)
    {
        _repo = repo;
        _acessoLog = acessoLog;
    }

    public async Task Handle(RegistrarRealizacaoCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var procedimento = await _repo.ObterPorIdOuNulo(cmd.ProcedimentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Procedimento não encontrado.");

        procedimento.RegistrarRealizacao(
            cmd.DataRealizada,
            cmd.DescricaoCirurgica,
            MapearFicha(cmd.FichaAnestesica),
            cmd.EvolucaoPosOp);

        await _repo.Salvar(procedimento);

        if (cmd.SolicitanteUsuarioId != Guid.Empty)
        {
            await _acessoLog.RegistrarAsync(
                procedimento.ProntuarioId, cmd.SolicitanteUsuarioId, cmd.EstabelecimentoId,
                TipoAcessoProntuario.Escrita);
        }
    }

    private static FichaAnestesica? MapearFicha(Imedto.Backend.Contracts.Cirurgias.FichaAnestesica? dto)
    {
        if (dto is null) return null;

        return new FichaAnestesica
        {
            Tecnica = dto.Tecnica,
            InicioAnestesia = dto.InicioAnestesia,
            FimAnestesia = dto.FimAnestesia,
            Drogas = dto.Drogas.Select(static d => new DrogaAnestesica
            {
                Nome = d.Nome,
                Dose = d.Dose,
                Via = d.Via,
                Hora = d.Hora
            }).ToList(),
            Intercorrencias = dto.Intercorrencias,
            Observacoes = dto.Observacoes,
            Monitorizacao = dto.Monitorizacao?.Select(static m => new MonitorizacaoVital
            {
                Hora = m.Hora,
                PressaoArterial = m.PressaoArterial,
                FrequenciaCardiaca = m.FrequenciaCardiaca,
                FrequenciaRespiratoria = m.FrequenciaRespiratoria,
                SaturacaoO2 = m.SaturacaoO2,
                Temperatura = m.Temperatura
            }).ToList()
        };
    }
}
