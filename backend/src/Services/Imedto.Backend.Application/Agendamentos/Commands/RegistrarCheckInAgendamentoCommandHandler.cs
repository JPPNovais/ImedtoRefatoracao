using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Cobrancas;
using Imedto.Backend.Domain.Convenios;
using Imedto.Backend.Domain.Salas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class RegistrarCheckInAgendamentoCommandHandler : ICommandHandler<RegistrarCheckInAgendamentoCommand>
{
    private readonly IAgendamentoRepository _agendamentoRepo;
    private readonly ISalaRepository _salaRepo;
    private readonly IAgendamentoSalaAuditRepository _auditRepo;
    private readonly ICobrancaRepository _cobrancaRepo;
    private readonly IConvenioRepository _convenioRepo;

    public RegistrarCheckInAgendamentoCommandHandler(
        IAgendamentoRepository agendamentoRepo,
        ISalaRepository salaRepo,
        IAgendamentoSalaAuditRepository auditRepo,
        ICobrancaRepository cobrancaRepo,
        IConvenioRepository convenioRepo)
    {
        _agendamentoRepo = agendamentoRepo;
        _salaRepo = salaRepo;
        _auditRepo = auditRepo;
        _cobrancaRepo = cobrancaRepo;
        _convenioRepo = convenioRepo;
    }

    public async Task Handle(RegistrarCheckInAgendamentoCommand cmd)
    {
        // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
        var agendamento = await _agendamentoRepo.ObterPorIdOuNulo(cmd.AgendamentoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Agendamento não encontrado.");

        if (cmd.SalaId.HasValue)
        {
            var sala = await _salaRepo.ObterPorIdOuNulo(cmd.SalaId.Value, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Sala não encontrada ou inativa.");
            if (!sala.Ativo)
                throw new BusinessException("Sala não encontrada ou inativa.");
        }

        var salaIdAnterior = agendamento.SalaId;
        agendamento.RegistrarCheckIn();
        if (cmd.SalaId.HasValue)
            agendamento.AlocarSala(cmd.SalaId);

        await _agendamentoRepo.Salvar(agendamento);

        // R1: Cria cobrança na mesma transação do check-in (UnitOfWork do controller)
        var tipoAtendimento = Enum.TryParse<TipoAtendimento>(cmd.TipoAtendimento, out var ta)
            ? ta
            : TipoAtendimento.Particular;

        // F6/R7: valida convenioId se informado (ativo do estabelecimento — 404 genérico se inválido/alheio)
        long? convenioId = null;
        if (tipoAtendimento == TipoAtendimento.Convenio && cmd.ConvenioId.HasValue)
        {
            var convenio = await _convenioRepo.ObterPorIdOuNulo(cmd.ConvenioId.Value, agendamento.EstabelecimentoId);
            if (convenio is not null && convenio.Ativo)
                convenioId = convenio.Id;
            // Se não encontrado/inativo: silencioso (convenioId permanece null — CA144)
        }

        var cobranca = Cobranca.CriarParaConsulta(
            estabelecimentoId: agendamento.EstabelecimentoId,
            pacienteId: agendamento.PacienteId,
            agendamentoId: agendamento.Id,
            tipoAtendimento: tipoAtendimento,
            valorCobrado: cmd.ValorCobrado,
            descricao: $"Consulta — {agendamento.TipoServico}",
            criadoPorUsuarioId: cmd.UsuarioSolicitanteId,
            convenioId: convenioId);

        await _cobrancaRepo.Salvar(cobranca);

        if (cmd.SalaId.HasValue && salaIdAnterior != cmd.SalaId)
        {
            await _auditRepo.Registrar(AgendamentoSalaAudit.Registrar(
                agendamento.Id,
                agendamento.EstabelecimentoId,
                salaIdAnterior,
                cmd.SalaId,
                cmd.UsuarioSolicitanteId));
        }
    }
}
