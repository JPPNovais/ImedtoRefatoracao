using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Estabelecimentos.Commands;

public class AtualizarEstabelecimentoCommandHandler : ICommandHandler<AtualizarEstabelecimentoCommand>
{
    private readonly IEstabelecimentoRepository _repository;
    private readonly IModeloPermissaoRepository _permissoes;

    public AtualizarEstabelecimentoCommandHandler(
        IEstabelecimentoRepository repository,
        IModeloPermissaoRepository permissoes)
    {
        _repository = repository;
        _permissoes = permissoes;
    }

    public async Task Handle(AtualizarEstabelecimentoCommand command)
    {
        var estab = await _repository.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        // Controle de acesso: Dono OU Admin com permissão extra `config_estabelecimento`.
        // UsuarioTemPermissaoExtra já trata Dono como pass-through.
        var podeEditar = await _permissoes.UsuarioTemPermissaoExtra(
            command.UsuarioSolicitanteId,
            command.EstabelecimentoId,
            PermissoesExtras.ConfigEstabelecimento);
        if (!podeEditar)
            throw new BusinessException("Você não tem permissão para alterar este estabelecimento.");

        var cnpjDigitos = new string((command.Cnpj ?? "").Where(char.IsDigit).ToArray());
        if (cnpjDigitos.Length > 0 && await _repository.ExisteCnpj(cnpjDigitos, estab.Id))
            throw new BusinessException("CNPJ já cadastrado em outro estabelecimento.");

        estab.AtualizarDados(
            command.NomeFantasia,
            command.RazaoSocial,
            command.Cnpj,
            command.Telefone,
            command.Endereco);

        // Cidade/UF foram movidas para AtualizarEndereco (validação de UF de 2 letras
        // vive no aggregate). Mantemos a chamada incondicional — passar null/whitespace
        // limpa o campo, que é o comportamento esperado quando o usuário esvazia o input.
        estab.AtualizarEndereco(command.Endereco, command.Cidade, command.Estado);

        await _repository.Salvar(estab);
    }
}
