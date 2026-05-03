using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Pacientes.Commands;

public class AtualizarPacienteCommandHandler : ICommandHandler<AtualizarPacienteCommand>
{
    private readonly IPacienteRepository _repository;
    private readonly IPacienteAcessoLogService _acessoLog;

    public AtualizarPacienteCommandHandler(
        IPacienteRepository repository,
        IPacienteAcessoLogService acessoLog)
    {
        _repository = repository;
        _acessoLog = acessoLog;
    }

    public async Task Handle(AtualizarPacienteCommand command)
    {
        // Defense-in-depth LGPD: o repositorio ja filtra por tenant — paciente de
        // outro estabelecimento retorna null. Mensagem nao vaza existencia.
        var paciente = await _repository.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        var cpfDigitos = new string((command.Cpf ?? "").Where(char.IsDigit).ToArray());
        if (!string.IsNullOrEmpty(cpfDigitos) &&
            await _repository.ExisteCpfNoEstabelecimento(cpfDigitos, command.EstabelecimentoId, ignorarPacienteId: command.PacienteId))
        {
            throw new BusinessException("Já existe outro paciente com este CPF neste estabelecimento.");
        }

        if (!Enum.TryParse<GeneroPaciente>(command.Genero, ignoreCase: true, out var genero))
            genero = GeneroPaciente.NaoInformado;

        paciente.AtualizarDados(
            command.NomeCompleto,
            command.Cpf,
            command.DataNascimento,
            genero,
            command.Telefone,
            command.Email,
            command.Endereco,
            command.Observacoes);

        await _repository.Salvar(paciente);

        // Audit LGPD: edicao de dados pessoais. SolicitanteUsuarioId vem do JWT
        // (controller seta), entao este eh o usuario que efetivamente fez a edicao.
        await _acessoLog.RegistrarAsync(
            command.PacienteId, command.SolicitanteUsuarioId, command.EstabelecimentoId, TipoAcessoPaciente.Edicao);
    }
}
