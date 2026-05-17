using Imedto.Backend.Contracts.Estabelecimentos.Commands;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.ModelosPermissao;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Estabelecimentos.Commands;

/// <summary>
/// Remove a foto/logo do estabelecimento. Autorização: Dono OU Admin com permissão extra
/// <c>config_estabelecimento</c> (mesma regra do controller via
/// <c>RequiresPermissaoExtra(ConfigEstabelecimento)</c>). Defense-in-depth.
///
/// Ordem das operações: apaga primeiro o blob no S3 e só depois zera <c>FotoUrl</c>
/// no aggregate. Se o S3 falhar, a foto continua referenciada no banco — usuário
/// vê erro e pode tentar de novo (vs. cenário inverso, em que banco zera mas o
/// blob fica órfão consumindo storage). O delete S3 é idempotente: se o objeto
/// já não existe, não lança.
/// </summary>
public class RemoverFotoEstabelecimentoCommandHandler : ICommandHandler<RemoverFotoEstabelecimentoCommand>
{
    private readonly IEstabelecimentoRepository _repository;
    private readonly IFotoStorageService _storage;
    private readonly IModeloPermissaoRepository _permissoes;

    public RemoverFotoEstabelecimentoCommandHandler(
        IEstabelecimentoRepository repository,
        IFotoStorageService storage,
        IModeloPermissaoRepository permissoes)
    {
        _repository = repository;
        _storage = storage;
        _permissoes = permissoes;
    }

    public async Task Handle(RemoverFotoEstabelecimentoCommand command)
    {
        var estab = await _repository.ObterPorIdOuNulo(command.EstabelecimentoId)
            ?? throw new BusinessException("Estabelecimento não encontrado.");

        // UsuarioTemPermissaoExtra já trata Dono como pass-through.
        var podeEditar = await _permissoes.UsuarioTemPermissaoExtra(
            command.UsuarioSolicitanteId,
            command.EstabelecimentoId,
            PermissoesExtras.ConfigEstabelecimento);
        if (!podeEditar)
            throw new BusinessException("Você não tem permissão para alterar este estabelecimento.");

        if (string.IsNullOrWhiteSpace(estab.FotoUrl))
        {
            // Idempotente: já não havia foto — não toca em nada (audit limpo).
            return;
        }

        // Deriva o path a partir da convenção usada no upload
        // (estabelecimentos/{id}.{ext}). Não confiamos na URL gravada porque ela
        // é presigned (com query string) e a extensão é o que importa para o key.
        var ext = ExtrairExtensaoDoUrl(estab.FotoUrl);
        var path = $"estabelecimentos/{command.EstabelecimentoId}.{ext}";

        await _storage.RemoverFotoAsync(path);

        estab.RemoverFoto();
        await _repository.Salvar(estab);
    }

    /// <summary>
    /// Lê a extensão do path da URL (ignora query string da presigned URL).
    /// Default "jpg" quando não dá pra inferir — coerente com o upload, que
    /// também faz fallback para "jpg" quando não tem extensão.
    /// </summary>
    private static string ExtrairExtensaoDoUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var nome = Path.GetFileName(uri.AbsolutePath);
            var ext = Path.GetExtension(nome).TrimStart('.').ToLowerInvariant();
            return string.IsNullOrWhiteSpace(ext) ? "jpg" : ext;
        }
        catch
        {
            return "jpg";
        }
    }
}
