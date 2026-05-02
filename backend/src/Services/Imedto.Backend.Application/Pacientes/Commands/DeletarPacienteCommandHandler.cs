using Microsoft.Extensions.Logging;
using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Pacientes.Commands;

public class DeletarPacienteCommandHandler : ICommandHandler<DeletarPacienteCommand>
{
    private readonly IPacienteRepository _repository;
    private readonly ILogger<DeletarPacienteCommandHandler> _logger;

    public DeletarPacienteCommandHandler(
        IPacienteRepository repository,
        ILogger<DeletarPacienteCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(DeletarPacienteCommand command)
    {
        // Defense-in-depth LGPD: o repositorio ja filtra por tenant — paciente de
        // outro estabelecimento retorna null. Mensagem nao vaza existencia.
        var paciente = await _repository.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        paciente.MarcarComoDeletado(command.SolicitanteUsuarioId);
        await _repository.Salvar(paciente);

        // Audit trail mínima (sem PII no log — só IDs).
        _logger.LogInformation(
            "LGPD: paciente deletado. Paciente={PacienteId}, Estabelecimento={EstabelecimentoId}, Solicitante={UsuarioId}",
            command.PacienteId, command.EstabelecimentoId, command.SolicitanteUsuarioId);
    }
}
