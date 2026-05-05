using Imedto.Backend.Contracts.Orcamentos.Catalogos.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Catalogos;

internal static class CatalogoTenantGuard
{
    public static void Verificar<T>(long entidadeEstabId, long requestEstabId)
    {
        if (entidadeEstabId != requestEstabId)
            throw new BusinessException("Registro não encontrado neste estabelecimento.");
    }
}

// ──────────── Cirurgias ────────────

public class CriarCatalogoCirurgiaCommandHandler : ICommandHandler<CriarCatalogoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaRepository _repo;
    public CriarCatalogoCirurgiaCommandHandler(ICatalogoCirurgiaRepository repo) => _repo = repo;

    public async Task Handle(CriarCatalogoCirurgiaCommand cmd)
    {
        var entity = CatalogoCirurgia.Criar(cmd.EstabelecimentoId, cmd.Descricao, cmd.ValorBase, cmd.DuracaoPadraoMinutos);
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }
}

public class AtualizarCatalogoCirurgiaCommandHandler : ICommandHandler<AtualizarCatalogoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaRepository _repo;
    public AtualizarCatalogoCirurgiaCommandHandler(ICatalogoCirurgiaRepository repo) => _repo = repo;

    public async Task Handle(AtualizarCatalogoCirurgiaCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id) ?? throw new BusinessException("Cirurgia não encontrada.");
        CatalogoTenantGuard.Verificar<CatalogoCirurgia>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        entity.Atualizar(cmd.Descricao, cmd.ValorBase, cmd.DuracaoPadraoMinutos);
        await _repo.Salvar(entity);
    }
}

public class RemoverCatalogoCirurgiaCommandHandler : ICommandHandler<RemoverCatalogoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaRepository _repo;
    public RemoverCatalogoCirurgiaCommandHandler(ICatalogoCirurgiaRepository repo) => _repo = repo;

    public async Task Handle(RemoverCatalogoCirurgiaCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id) ?? throw new BusinessException("Cirurgia não encontrada.");
        CatalogoTenantGuard.Verificar<CatalogoCirurgia>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        // Soft-delete via Inativar — preserva histórico de orçamentos antigos.
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

// ──────────── Valor profissional ────────────

public class CriarValorProfissionalCommandHandler : ICommandHandler<CriarValorProfissionalCommand>
{
    private readonly IValorProfissionalOrcamentoRepository _repo;
    public CriarValorProfissionalCommandHandler(IValorProfissionalOrcamentoRepository repo) => _repo = repo;

    public async Task Handle(CriarValorProfissionalCommand cmd)
    {
        var entity = ValorProfissionalOrcamento.Criar(
            cmd.EstabelecimentoId, cmd.ProfissionalUsuarioId, cmd.Funcao,
            cmd.TempoBaseMinutos, cmd.ValorTempoBase,
            cmd.TempoAdicionalMinutos, cmd.ValorAdicional, cmd.ValorPlus);
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }
}

public class AtualizarValorProfissionalCommandHandler : ICommandHandler<AtualizarValorProfissionalCommand>
{
    private readonly IValorProfissionalOrcamentoRepository _repo;
    public AtualizarValorProfissionalCommandHandler(IValorProfissionalOrcamentoRepository repo) => _repo = repo;

    public async Task Handle(AtualizarValorProfissionalCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id) ?? throw new BusinessException("Valor profissional não encontrado.");
        CatalogoTenantGuard.Verificar<ValorProfissionalOrcamento>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        entity.Atualizar(cmd.Funcao, cmd.TempoBaseMinutos, cmd.ValorTempoBase,
            cmd.TempoAdicionalMinutos, cmd.ValorAdicional, cmd.ValorPlus);
        await _repo.Salvar(entity);
    }
}

public class RemoverValorProfissionalCommandHandler : ICommandHandler<RemoverValorProfissionalCommand>
{
    private readonly IValorProfissionalOrcamentoRepository _repo;
    public RemoverValorProfissionalCommandHandler(IValorProfissionalOrcamentoRepository repo) => _repo = repo;

    public async Task Handle(RemoverValorProfissionalCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id) ?? throw new BusinessException("Valor profissional não encontrado.");
        CatalogoTenantGuard.Verificar<ValorProfissionalOrcamento>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

// ──────────── Configuração local cirurgia (upsert por tipo) ────────────

public class SalvarConfiguracaoLocalCommandHandler : ICommandHandler<SalvarConfiguracaoLocalCommand>
{
    private readonly IConfiguracaoLocalCirurgiaRepository _repo;
    public SalvarConfiguracaoLocalCommandHandler(IConfiguracaoLocalCirurgiaRepository repo) => _repo = repo;

    public async Task Handle(SalvarConfiguracaoLocalCommand cmd)
    {
        if (!Enum.TryParse<TipoInternacao>(cmd.TipoInternacao, ignoreCase: true, out var tipo))
            throw new BusinessException($"Tipo de internação '{cmd.TipoInternacao}' inválido.");

        var entity = await _repo.ObterPorEstabelecimentoETipo(cmd.EstabelecimentoId, tipo);
        if (entity is null)
        {
            entity = ConfiguracaoLocalCirurgia.Criar(cmd.EstabelecimentoId, tipo,
                cmd.TempoBaseMinutos, cmd.ValorBase,
                cmd.TempoAdicionalMinutos, cmd.ValorAdicional);
        }
        else
        {
            entity.Atualizar(cmd.TempoBaseMinutos, cmd.ValorBase,
                cmd.TempoAdicionalMinutos, cmd.ValorAdicional);
        }
        await _repo.Salvar(entity);
        cmd.IdSalvo = entity.Id;
    }
}

// ──────────── Equipes ────────────

public class CriarCatalogoEquipeCommandHandler : ICommandHandler<CriarCatalogoEquipeCommand>
{
    private readonly ICatalogoEquipeEspecializadaRepository _repo;
    public CriarCatalogoEquipeCommandHandler(ICatalogoEquipeEspecializadaRepository repo) => _repo = repo;

    public async Task Handle(CriarCatalogoEquipeCommand cmd)
    {
        var entity = CatalogoEquipeEspecializada.Criar(cmd.EstabelecimentoId, cmd.Descricao, cmd.ValorPadrao);
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }
}

public class AtualizarCatalogoEquipeCommandHandler : ICommandHandler<AtualizarCatalogoEquipeCommand>
{
    private readonly ICatalogoEquipeEspecializadaRepository _repo;
    public AtualizarCatalogoEquipeCommandHandler(ICatalogoEquipeEspecializadaRepository repo) => _repo = repo;

    public async Task Handle(AtualizarCatalogoEquipeCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id) ?? throw new BusinessException("Equipe não encontrada.");
        CatalogoTenantGuard.Verificar<CatalogoEquipeEspecializada>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        entity.Atualizar(cmd.Descricao, cmd.ValorPadrao);
        await _repo.Salvar(entity);
    }
}

public class RemoverCatalogoEquipeCommandHandler : ICommandHandler<RemoverCatalogoEquipeCommand>
{
    private readonly ICatalogoEquipeEspecializadaRepository _repo;
    public RemoverCatalogoEquipeCommandHandler(ICatalogoEquipeEspecializadaRepository repo) => _repo = repo;

    public async Task Handle(RemoverCatalogoEquipeCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id) ?? throw new BusinessException("Equipe não encontrada.");
        CatalogoTenantGuard.Verificar<CatalogoEquipeEspecializada>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

// ──────────── Implantes ────────────

public class CriarCatalogoImplanteCommandHandler : ICommandHandler<CriarCatalogoImplanteCommand>
{
    private readonly ICatalogoImplanteRepository _repo;
    private readonly IItemInventarioRepository _inventarioRepo;
    public CriarCatalogoImplanteCommandHandler(ICatalogoImplanteRepository repo, IItemInventarioRepository inventarioRepo)
    {
        _repo = repo;
        _inventarioRepo = inventarioRepo;
    }

    public async Task Handle(CriarCatalogoImplanteCommand cmd)
    {
        if (cmd.ItemInventarioId is { } invId)
        {
            // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
            _ = await _inventarioRepo.ObterPorIdOuNulo(invId, cmd.EstabelecimentoId)
                ?? throw new BusinessException($"Item de inventário {invId} não encontrado.");
        }

        var entity = CatalogoImplante.Criar(cmd.EstabelecimentoId, cmd.ItemInventarioId, cmd.Descricao, cmd.CustoUnitario);
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }
}

public class AtualizarCatalogoImplanteCommandHandler : ICommandHandler<AtualizarCatalogoImplanteCommand>
{
    private readonly ICatalogoImplanteRepository _repo;
    private readonly IItemInventarioRepository _inventarioRepo;
    public AtualizarCatalogoImplanteCommandHandler(ICatalogoImplanteRepository repo, IItemInventarioRepository inventarioRepo)
    {
        _repo = repo;
        _inventarioRepo = inventarioRepo;
    }

    public async Task Handle(AtualizarCatalogoImplanteCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id) ?? throw new BusinessException("Implante não encontrado.");
        CatalogoTenantGuard.Verificar<CatalogoImplante>(entity.EstabelecimentoId, cmd.EstabelecimentoId);

        if (cmd.ItemInventarioId is { } invId)
        {
            // Defense-in-depth multi-tenant: filtro por estabelecimentoId no proprio repo.
            _ = await _inventarioRepo.ObterPorIdOuNulo(invId, cmd.EstabelecimentoId)
                ?? throw new BusinessException($"Item de inventário {invId} não encontrado.");
        }

        entity.Atualizar(cmd.ItemInventarioId, cmd.Descricao, cmd.CustoUnitario);
        await _repo.Salvar(entity);
    }
}

public class RemoverCatalogoImplanteCommandHandler : ICommandHandler<RemoverCatalogoImplanteCommand>
{
    private readonly ICatalogoImplanteRepository _repo;
    public RemoverCatalogoImplanteCommandHandler(ICatalogoImplanteRepository repo) => _repo = repo;

    public async Task Handle(RemoverCatalogoImplanteCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id) ?? throw new BusinessException("Implante não encontrado.");
        CatalogoTenantGuard.Verificar<CatalogoImplante>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

// ──────────── Configuração pagamento ────────────

public class CriarConfiguracaoPagamentoCommandHandler : ICommandHandler<CriarConfiguracaoPagamentoCommand>
{
    private readonly IConfiguracaoPagamentoCatalogoRepository _repo;
    public CriarConfiguracaoPagamentoCommandHandler(IConfiguracaoPagamentoCatalogoRepository repo) => _repo = repo;

    public async Task Handle(CriarConfiguracaoPagamentoCommand cmd)
    {
        var entity = ConfiguracaoPagamentoCatalogo.Criar(
            cmd.EstabelecimentoId, cmd.FormaPagamentoId,
            cmd.AcrescimoPercentual, cmd.EntradaPercentualPadrao,
            cmd.TaxaParcela, cmd.ParcelasMaximas);
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }
}

public class AtualizarConfiguracaoPagamentoCommandHandler : ICommandHandler<AtualizarConfiguracaoPagamentoCommand>
{
    private readonly IConfiguracaoPagamentoCatalogoRepository _repo;
    public AtualizarConfiguracaoPagamentoCommandHandler(IConfiguracaoPagamentoCatalogoRepository repo) => _repo = repo;

    public async Task Handle(AtualizarConfiguracaoPagamentoCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id)
            ?? throw new BusinessException("Configuração de pagamento não encontrada.");
        CatalogoTenantGuard.Verificar<ConfiguracaoPagamentoCatalogo>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        entity.Atualizar(cmd.AcrescimoPercentual, cmd.EntradaPercentualPadrao, cmd.TaxaParcela, cmd.ParcelasMaximas);
        await _repo.Salvar(entity);
    }
}

// ──────────── Produtos ────────────

public class CriarCatalogoProdutoCommandHandler : ICommandHandler<CriarCatalogoProdutoCommand>
{
    private readonly ICatalogoProdutoRepository _repo;
    public CriarCatalogoProdutoCommandHandler(ICatalogoProdutoRepository repo) => _repo = repo;

    public async Task Handle(CriarCatalogoProdutoCommand cmd)
    {
        var entity = CatalogoProduto.Criar(cmd.EstabelecimentoId, cmd.Nome, cmd.Descricao, cmd.ValorReferencia, cmd.UsoUnico);
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }
}

public class AtualizarCatalogoProdutoCommandHandler : ICommandHandler<AtualizarCatalogoProdutoCommand>
{
    private readonly ICatalogoProdutoRepository _repo;
    public AtualizarCatalogoProdutoCommandHandler(ICatalogoProdutoRepository repo) => _repo = repo;

    public async Task Handle(AtualizarCatalogoProdutoCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id) ?? throw new BusinessException("Produto não encontrado.");
        CatalogoTenantGuard.Verificar<CatalogoProduto>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        entity.Atualizar(cmd.Nome, cmd.Descricao, cmd.ValorReferencia, cmd.UsoUnico);
        await _repo.Salvar(entity);
    }
}

public class RemoverCatalogoProdutoCommandHandler : ICommandHandler<RemoverCatalogoProdutoCommand>
{
    private readonly ICatalogoProdutoRepository _repo;
    public RemoverCatalogoProdutoCommandHandler(ICatalogoProdutoRepository repo) => _repo = repo;

    public async Task Handle(RemoverCatalogoProdutoCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id) ?? throw new BusinessException("Produto não encontrado.");
        CatalogoTenantGuard.Verificar<CatalogoProduto>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

// ──────────── Vínculo cirurgia × produto ────────────

public class VincularProdutoCirurgiaCommandHandler : ICommandHandler<VincularProdutoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaRepository _cirRepo;
    private readonly ICatalogoProdutoRepository _prodRepo;
    private readonly ICatalogoCirurgiaProdutoRepository _vincRepo;
    public VincularProdutoCirurgiaCommandHandler(
        ICatalogoCirurgiaRepository cirRepo,
        ICatalogoProdutoRepository prodRepo,
        ICatalogoCirurgiaProdutoRepository vincRepo)
    {
        _cirRepo = cirRepo;
        _prodRepo = prodRepo;
        _vincRepo = vincRepo;
    }

    public async Task Handle(VincularProdutoCirurgiaCommand cmd)
    {
        // Tenant guard via cirurgia + produto.
        var cirurgia = await _cirRepo.ObterPorIdOuNulo(cmd.CatalogoCirurgiaId)
            ?? throw new BusinessException("Cirurgia não encontrada.");
        if (cirurgia.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Cirurgia não pertence a este estabelecimento.");

        var produto = await _prodRepo.ObterPorIdOuNulo(cmd.CatalogoProdutoId)
            ?? throw new BusinessException("Produto não encontrado.");
        if (produto.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Produto não pertence a este estabelecimento.");

        // Idempotência: se já existe o vínculo, atualiza qtd em vez de duplicar.
        var existente = await _vincRepo.ObterPorCirurgiaProduto(cmd.CatalogoCirurgiaId, cmd.CatalogoProdutoId);
        if (existente is not null)
        {
            existente.AtualizarQuantidade(cmd.QuantidadePadrao, cmd.Obrigatorio);
            await _vincRepo.Salvar(existente);
            cmd.IdCriado = existente.Id;
            return;
        }

        var vinc = CatalogoCirurgiaProduto.Criar(cmd.CatalogoCirurgiaId, cmd.CatalogoProdutoId, cmd.QuantidadePadrao, cmd.Obrigatorio);
        await _vincRepo.Salvar(vinc);
        cmd.IdCriado = vinc.Id;
    }
}

public class AtualizarVinculoProdutoCirurgiaCommandHandler : ICommandHandler<AtualizarVinculoProdutoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaProdutoRepository _vincRepo;
    private readonly ICatalogoCirurgiaRepository _cirRepo;
    public AtualizarVinculoProdutoCirurgiaCommandHandler(
        ICatalogoCirurgiaProdutoRepository vincRepo,
        ICatalogoCirurgiaRepository cirRepo)
    {
        _vincRepo = vincRepo;
        _cirRepo = cirRepo;
    }

    public async Task Handle(AtualizarVinculoProdutoCirurgiaCommand cmd)
    {
        var vinc = await _vincRepo.ObterPorIdOuNulo(cmd.VinculoId)
            ?? throw new BusinessException("Vínculo não encontrado.");
        var cirurgia = await _cirRepo.ObterPorIdOuNulo(vinc.CatalogoCirurgiaId)
            ?? throw new BusinessException("Cirurgia não encontrada.");
        if (cirurgia.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Vínculo não pertence a este estabelecimento.");

        vinc.AtualizarQuantidade(cmd.QuantidadePadrao, cmd.Obrigatorio);
        await _vincRepo.Salvar(vinc);
    }
}

public class DesvincularProdutoCirurgiaCommandHandler : ICommandHandler<DesvincularProdutoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaProdutoRepository _vincRepo;
    private readonly ICatalogoCirurgiaRepository _cirRepo;
    public DesvincularProdutoCirurgiaCommandHandler(
        ICatalogoCirurgiaProdutoRepository vincRepo,
        ICatalogoCirurgiaRepository cirRepo)
    {
        _vincRepo = vincRepo;
        _cirRepo = cirRepo;
    }

    public async Task Handle(DesvincularProdutoCirurgiaCommand cmd)
    {
        var vinc = await _vincRepo.ObterPorIdOuNulo(cmd.VinculoId)
            ?? throw new BusinessException("Vínculo não encontrado.");
        var cirurgia = await _cirRepo.ObterPorIdOuNulo(vinc.CatalogoCirurgiaId)
            ?? throw new BusinessException("Cirurgia não encontrada.");
        if (cirurgia.EstabelecimentoId != cmd.EstabelecimentoId)
            throw new BusinessException("Vínculo não pertence a este estabelecimento.");
        await _vincRepo.Remover(vinc);
    }
}

public class RemoverConfiguracaoPagamentoCommandHandler : ICommandHandler<RemoverConfiguracaoPagamentoCommand>
{
    private readonly IConfiguracaoPagamentoCatalogoRepository _repo;
    public RemoverConfiguracaoPagamentoCommandHandler(IConfiguracaoPagamentoCatalogoRepository repo) => _repo = repo;

    public async Task Handle(RemoverConfiguracaoPagamentoCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id)
            ?? throw new BusinessException("Configuração de pagamento não encontrada.");
        CatalogoTenantGuard.Verificar<ConfiguracaoPagamentoCatalogo>(entity.EstabelecimentoId, cmd.EstabelecimentoId);
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}
