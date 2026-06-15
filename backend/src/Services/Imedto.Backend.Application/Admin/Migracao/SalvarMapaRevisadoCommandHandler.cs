using System.Text.Json;
using Imedto.Backend.Contracts.Admin.Migracao;
using Imedto.Backend.Domain.Migracao;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Migracao;

/// <summary>
/// Persiste o mapa revisado pelo admin e aciona revisão no aggregate MigracaoMapa.
/// Scoped — usa EF via repositório.
/// </summary>
public sealed class SalvarMapaRevisadoCommandHandler
{
    private readonly IMigracaoMapaRepository _mapaRepo;

    public SalvarMapaRevisadoCommandHandler(IMigracaoMapaRepository mapaRepo)
    {
        _mapaRepo = mapaRepo;
    }

    public async Task Handle(SalvarMapaRevisadoCommand cmd, CancellationToken ct = default)
    {
        if (cmd.JobId <= 0) throw new BusinessException("Job inválido.");
        if (string.IsNullOrWhiteSpace(cmd.Entidade)) throw new BusinessException("Entidade inválida.");
        if (cmd.RevisadoPorUsuarioId == Guid.Empty) throw new BusinessException("Usuário revisor é obrigatório.");

        // Busca mapa existente — o mapa é criado pelo job de inferência, admin apenas revisa.
        // EstabelecimentoId: para revisão admin, não filtramos por tenant (admin vê tudo).
        // A solução: buscar sem tenant — adicionamos método na porta.
        var mapa = await _mapaRepo.ObterPorJobEEntidadeAdminOuNulo(cmd.JobId, cmd.Entidade, ct)
            ?? throw new BusinessException("Mapa não encontrado.");

        // Serializa o novo de_para como JSON mantendo estrutura.
        var mapaJson = JsonSerializer.Serialize(new
        {
            de_para   = cmd.DePara,
            confianca = 1.0, // revisão manual = confiança máxima
            duvidas   = Array.Empty<string>(),
        });

        mapa.Revisar(mapaJson, cmd.RevisadoPorUsuarioId);
        await _mapaRepo.Salvar(mapa, ct);
    }
}
