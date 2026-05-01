using Imedto.Backend.Contracts.Agendamentos.Commands;
using Imedto.Backend.Domain.Agendamentos;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Agendamentos.Commands;

public class AdicionarListaEsperaCommandHandler : ICommandHandler<AdicionarListaEsperaCommand>
{
    private readonly IListaEsperaRepository _repo;
    private readonly IPacienteRepository _pacienteRepo;

    public AdicionarListaEsperaCommandHandler(
        IListaEsperaRepository repo,
        IPacienteRepository pacienteRepo)
    {
        _repo = repo;
        _pacienteRepo = pacienteRepo;
    }

    public async Task Handle(AdicionarListaEsperaCommand cmd)
    {
        var paciente = await _pacienteRepo.ObterPorId(cmd.PacienteId);
        if (paciente.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Paciente não pertence a este estabelecimento.");

        if (!Enum.TryParse<ListaEsperaPrioridade>(cmd.Prioridade, ignoreCase: true, out var prioridade))
            throw new BusinessException($"Prioridade '{cmd.Prioridade}' inválida.");
        if (!Enum.TryParse<ListaEsperaPreferenciaPeriodo>(cmd.PreferenciaPeriodo, ignoreCase: true, out var periodo))
            throw new BusinessException($"Preferência de período '{cmd.PreferenciaPeriodo}' inválida.");

        var entity = ListaEsperaAgendamento.Criar(
            cmd.EstabelecimentoId,
            cmd.PacienteId,
            cmd.Motivo,
            cmd.ProfissionalPreferidoId,
            cmd.CriadoPorUsuarioId,
            prioridade,
            periodo);

        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }
}

public class RemoverListaEsperaCommandHandler : ICommandHandler<RemoverListaEsperaCommand>
{
    private readonly IListaEsperaRepository _repo;
    public RemoverListaEsperaCommandHandler(IListaEsperaRepository repo) => _repo = repo;

    public async Task Handle(RemoverListaEsperaCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id)
            ?? throw new BusinessException("Entrada da lista de espera não encontrada.");
        if (entity.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Entrada não pertence a este estabelecimento.");
        await _repo.Remover(entity);
    }
}
