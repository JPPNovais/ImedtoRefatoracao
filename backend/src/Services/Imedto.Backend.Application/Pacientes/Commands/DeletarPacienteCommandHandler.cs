using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Pacientes.Commands;

public class DeletarPacienteCommandHandler : ICommandHandler<DeletarPacienteCommand>
{
    private readonly IPacienteRepository _repository;
    private readonly IPacienteAcessoLogService _acessoLog;

    public DeletarPacienteCommandHandler(
        IPacienteRepository repository,
        IPacienteAcessoLogService acessoLog)
    {
        _repository = repository;
        _acessoLog = acessoLog;
    }

    public async Task Handle(DeletarPacienteCommand command)
    {
        // Defense-in-depth LGPD: o repositorio ja filtra por tenant — paciente de
        // outro estabelecimento retorna null. Mensagem nao vaza existencia.
        var paciente = await _repository.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        paciente.MarcarComoDeletado(command.SolicitanteUsuarioId);
        await _repository.Salvar(paciente);

        // Audit LGPD persistido em paciente_acesso_log (substitui o log
        // estruturado anterior — agora temos trilha imutavel em tabela).
        await _acessoLog.RegistrarAsync(
            command.PacienteId, command.SolicitanteUsuarioId, command.EstabelecimentoId, TipoAcessoPaciente.Exclusao);
    }
}
