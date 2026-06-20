using Imedto.Backend.Contracts.Pacientes.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Text;

namespace Imedto.Backend.Application.Pacientes.Commands;

public class AtualizarDadosBasicosPacienteCommandHandler : ICommandHandler<AtualizarDadosBasicosPacienteCommand>
{
    private readonly IPacienteRepository _repository;
    private readonly IPacienteAcessoLogService _acessoLog;

    public AtualizarDadosBasicosPacienteCommandHandler(
        IPacienteRepository repository,
        IPacienteAcessoLogService acessoLog)
    {
        _repository = repository;
        _acessoLog = acessoLog;
    }

    public async Task Handle(AtualizarDadosBasicosPacienteCommand command)
    {
        // Defense-in-depth LGPD: repositório filtra por tenant — paciente de outro
        // estabelecimento retorna null. Mensagem genérica não vaza existência.
        var paciente = await _repository.ObterPorIdOuNulo(command.PacienteId, command.EstabelecimentoId)
            ?? throw new BusinessException("Paciente não encontrado.");

        // Validação de CPF duplicado — só quando cpf foi explicitamente enviado e não é vazio
        if (command.CpfFoiEnviado && !string.IsNullOrWhiteSpace(command.Cpf))
        {
            var cpfDigitos = TextSanitizer.SomenteDigitos(command.Cpf);
            if (await _repository.ExisteCpfNoEstabelecimento(cpfDigitos, command.EstabelecimentoId, ignorarPacienteId: command.PacienteId))
                throw new BusinessException("Já existe outro paciente com este CPF neste estabelecimento.");
        }

        paciente.AtualizarDadosBasicos(
            command.NomeCompleto,
            command.Telefone,
            command.Email,
            command.DataNascimento,
            command.DataNascimentoFoiEnviada,
            command.Cpf,
            command.CpfFoiEnviado);

        await _repository.Salvar(paciente);

        // Audit LGPD: edição de dados pessoais.
        await _acessoLog.RegistrarAsync(
            command.PacienteId, command.SolicitanteUsuarioId, command.EstabelecimentoId, TipoAcessoPaciente.Edicao);
    }
}
