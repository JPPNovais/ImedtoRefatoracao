using Imedto.Backend.Contracts.Profissionais.Commands;
using Imedto.Backend.Domain.Common;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Profissionais.Commands;

/// <summary>
/// Remove a foto do profissional autenticado. Espelha o padrão de
/// <see cref="Imedto.Backend.Application.Estabelecimentos.Commands.RemoverFotoEstabelecimentoCommandHandler"/>.
///
/// Ordem das operações: apaga o blob no S3 ANTES de zerar <c>FotoUrl</c> no
/// aggregate. Se o S3 falhar, a foto continua referenciada no banco — usuário
/// vê erro e pode tentar de novo (vs. cenário inverso, em que banco zera mas
/// o blob fica órfão consumindo storage). O delete S3 é idempotente.
///
/// Autorização: garantida no controller — <see cref="RemoverFotoProfissionalCommand.UsuarioId"/>
/// é sempre <c>ICurrentUser</c>, nunca de parâmetro externo. Profissional só
/// mexe na própria foto.
/// </summary>
public class RemoverFotoProfissionalCommandHandler : ICommandHandler<RemoverFotoProfissionalCommand>
{
    private readonly IProfissionalRepository _repository;
    private readonly IFotoStorageService _storage;

    public RemoverFotoProfissionalCommandHandler(
        IProfissionalRepository repository,
        IFotoStorageService storage)
    {
        _repository = repository;
        _storage = storage;
    }

    public async Task Handle(RemoverFotoProfissionalCommand command)
    {
        if (command.UsuarioId == Guid.Empty)
            throw new BusinessException("Usuário não identificado.");

        var prof = await _repository.ObterPorIdOuNulo(command.UsuarioId)
            ?? throw new BusinessException("Perfil profissional não encontrado.");

        if (string.IsNullOrWhiteSpace(prof.FotoUrl))
        {
            // Idempotente: já não havia foto — não toca em nada (audit limpo).
            return;
        }

        // Deriva o path da convenção usada no upload (profissionais/{usuarioId}.{ext}).
        // Não confiamos na URL gravada porque ela é presigned (query string).
        var ext = ExtrairExtensaoDoUrl(prof.FotoUrl);
        var path = $"profissionais/{command.UsuarioId}.{ext}";

        await _storage.RemoverFotoAsync(path);

        prof.RemoverFoto();
        await _repository.Salvar(prof);
    }

    /// <summary>
    /// Lê a extensão do path da URL (ignora query string da presigned URL).
    /// Default "jpg" — coerente com o upload, que também faz fallback para "jpg".
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
