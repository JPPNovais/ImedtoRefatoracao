using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Estabelecimentos.Commands;

public class AtualizarEstabelecimentoCommandHandler : ICommandHandler<AtualizarEstabelecimentoCommand>
{
    private readonly IEstabelecimentoRepository _repository;

    public AtualizarEstabelecimentoCommandHandler(IEstabelecimentoRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AtualizarEstabelecimentoCommand command)
    {
        var estab = await _repository.ObterPorId(command.EstabelecimentoId);

        // Controle de acesso: só o dono pode editar o estabelecimento (na Fase 5 isso vira
        // uma permissão granular via vínculo profissional).
        if (estab.DonoUsuarioId != command.UsuarioSolicitanteId)
            throw new BusinessException("Apenas o dono do estabelecimento pode editá-lo.");

        var cnpjDigitos = new string((command.Cnpj ?? "").Where(char.IsDigit).ToArray());
        if (cnpjDigitos.Length > 0 && await _repository.ExisteCnpj(cnpjDigitos, estab.Id))
            throw new BusinessException("CNPJ já cadastrado em outro estabelecimento.");

        estab.AtualizarDados(
            command.NomeFantasia,
            command.RazaoSocial,
            command.Cnpj,
            command.Telefone,
            command.Endereco);

        await _repository.Salvar(estab);
    }
}
