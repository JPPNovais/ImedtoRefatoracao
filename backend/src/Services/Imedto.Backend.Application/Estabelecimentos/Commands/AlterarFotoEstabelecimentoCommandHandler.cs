using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Estabelecimentos.Commands;

public class AlterarFotoEstabelecimentoCommandHandler : ICommandHandler<AlterarFotoEstabelecimentoCommand>
{
    private readonly IEstabelecimentoRepository _repository;
    private readonly IFotoStorageService _storage;
    private readonly IModeloPermissaoRepository _permissoes;

    public AlterarFotoEstabelecimentoCommandHandler(
        IEstabelecimentoRepository repository,
        IFotoStorageService storage,
        IModeloPermissaoRepository permissoes)
    {
        _repository = repository;
        _storage = storage;
        _permissoes = permissoes;
    }

    public async Task Handle(AlterarFotoEstabelecimentoCommand command)
    {
        var estab = await _repository.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        // Defense-in-depth: o controller já bloqueia via [RequiresPermissaoExtra(ConfigEstabelecimento)],
        // mas o handler revalida para que chamadas internas/bus em outros pontos não bypassem.
        // UsuarioTemPermissaoExtra já trata o dono como pass-through — Dono OU Admin com permissão extra.
        // TODO: extrair quando a regra crescer (mesma verificação está em RemoverFoto/Atualizar/AtualizarFuncionamento).
        var podeEditar = await _permissoes.UsuarioTemPermissaoExtra(
            command.UsuarioSolicitanteId,
            command.EstabelecimentoId,
            PermissoesExtras.ConfigEstabelecimento);
        if (!podeEditar)
            throw new BusinessException("Você não tem permissão para alterar este estabelecimento.");

        var ext = string.IsNullOrWhiteSpace(command.Extensao) ? "jpg" : command.Extensao.Trim().TrimStart('.').ToLowerInvariant();
        var path = $"estabelecimentos/{command.EstabelecimentoId}.{ext}";

        var url = await _storage.UploadFotoAsync(path, command.Conteudo, command.MimeType);

        estab.AlterarFoto(url);
        await _repository.Salvar(estab);
    }
}
