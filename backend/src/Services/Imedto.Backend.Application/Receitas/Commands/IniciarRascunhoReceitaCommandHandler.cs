using Imedto.Backend.Contracts.Receitas.Commands;
using Imedto.Backend.Domain.Pacientes;
using Imedto.Backend.Domain.Prontuarios;
using Imedto.Backend.Domain.Receitas;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Receitas.Commands;

/// <summary>
/// Cria uma receita em status <c>Rascunho</c>. Não faz audit de Escrita aqui:
/// rascunho é trabalho em andamento — só vira Escrita quando finalizar.
/// </summary>
public class IniciarRascunhoReceitaCommandHandler : ICommandHandler<IniciarRascunhoReceitaCommand>
{
    private readonly IReceitaRepository _receitaRepo;
    private readonly IProntuarioRepository _prontuarioRepo;
    private readonly IPacienteRepository _pacienteRepo;

    public IniciarRascunhoReceitaCommandHandler(
        IReceitaRepository receitaRepo,
        IProntuarioRepository prontuarioRepo,
        IPacienteRepository pacienteRepo)
    {
        _receitaRepo = receitaRepo;
        _prontuarioRepo = prontuarioRepo;
        _pacienteRepo = pacienteRepo;
    }

    public async Task Handle(IniciarRascunhoReceitaCommand cmd)
    {
        var paciente = await _pacienteRepo.ObterPorId(cmd.PacienteId);
        if (paciente.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Paciente não pertence a este estabelecimento.");
        if (paciente.EstaDeletado)
            throw new BusinessException("Paciente deletado — não é possível iniciar receita.");

        var prontuario = await _prontuarioRepo.ObterPorPaciente(cmd.PacienteId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Prontuário ainda não foi iniciado para este paciente.");

        var tipo = ReceitaParsers.ParseTipo(cmd.Tipo);
        var tipoNotificacao = ReceitaParsers.ParseTipoNotificacao(cmd.TipoNotificacao);
        var itensRicos = cmd.Itens.Select(ReceitaParsers.ToInput);

        var rascunho = Receita.IniciarRascunho(
            prontuario.Id,
            paciente.Id,
            cmd.ProfissionalUsuarioId,
            cmd.EstabelecimentoId,
            tipo,
            tipoNotificacao,
            cmd.Observacoes,
            cmd.ValidadeAte,
            itensRicos);

        await _receitaRepo.Salvar(rascunho);
        cmd.ReceitaIdCriada = rascunho.Id;
    }
}
