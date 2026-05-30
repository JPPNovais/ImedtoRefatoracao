using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries;
using Imedto.Backend.Contracts.Admin.Estabelecimentos.Queries.Results;
using Imedto.Backend.Domain.Admin;
using Imedto.Backend.Infrastructure.Admin;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Admin.Estabelecimentos.Queries;

/// <summary>
/// Revela CPF completo do dono. Motivo obrigatório (mín. 10 chars).
/// Gera audit obrigatório (CA17–CA19). Falha de audit = falha da operação.
/// Scoped: depende de ImedtoAdminAuditWriter (scoped).
/// </summary>
public class RevelarCpfDonoQueryHandler
    : IRequestHandler<RevelarCpfDonoQuery, CpfDonoReveladoDto>
{
    private readonly IAdminEstabelecimentosQueryRepository _repo;
    private readonly ImedtoAdminAuditWriter _audit;

    public RevelarCpfDonoQueryHandler(
        IAdminEstabelecimentosQueryRepository repo,
        ImedtoAdminAuditWriter audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<CpfDonoReveladoDto> Handle(RevelarCpfDonoQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Motivo) || query.Motivo.Trim().Length < 10)
            throw new BusinessException("Motivo é obrigatório e deve ter ao menos 10 caracteres.");

        var (cpfBruto, _) = await _repo.ObterCpfENomeFantasiaAsync(query.EstabelecimentoId);

        if (cpfBruto is null)
            throw new BusinessException("Estabelecimento não encontrado.");

        // Audit obrigatório em mutação de dado sensível — falha bloqueia operação.
        await _audit.RegistrarAsync(
            AcoesAuditAdmin.RevelarCpfDono,
            query.AdminId,
            "Estabelecimento",
            query.EstabelecimentoId.ToString(),
            tenantAfetadoId: query.EstabelecimentoId,
            motivo: query.Motivo.Trim());

        // Formata CPF com pontuação padrão: "123.456.789-00"
        var digits = new string(cpfBruto.Where(char.IsDigit).ToArray());
        var cpfFormatado = digits.Length == 11
            ? $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}"
            : cpfBruto;

        return new CpfDonoReveladoDto { Cpf = cpfFormatado };
    }
}
