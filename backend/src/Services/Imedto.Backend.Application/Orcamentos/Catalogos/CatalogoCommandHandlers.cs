using Imedto.Backend.Contracts.Orcamentos.Catalogos.Commands;
using Imedto.Backend.Domain.Inventario;
using Imedto.Backend.Domain.Orcamentos;
using Imedto.Backend.Domain.Orcamentos.Catalogos;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Application.Orcamentos.Catalogos;

public class CriarCatalogoCirurgiaCommandHandler : ICommandHandler<CriarCatalogoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaRepository _repo;
    public CriarCatalogoCirurgiaCommandHandler(ICatalogoCirurgiaRepository repo) => _repo = repo;

    public async Task Handle(CriarCatalogoCirurgiaCommand cmd)
    {
        var entity = CatalogoCirurgia.Criar(cmd.EstabelecimentoId, cmd.Descricao, cmd.ValorBase,
            cmd.DuracaoPadraoMinutos, cmd.CodigoInterno, cmd.CodigoTuss, cmd.Categoria);
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
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Cirurgia não encontrada.");
        entity.Atualizar(cmd.Descricao, cmd.ValorBase, cmd.DuracaoPadraoMinutos,
            cmd.CodigoInterno, cmd.CodigoTuss, cmd.Categoria);
        await _repo.Salvar(entity);
    }
}

public class RemoverCatalogoCirurgiaCommandHandler : ICommandHandler<RemoverCatalogoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaRepository _repo;
    private readonly IOrcamentoPacoteRepository _pacoteRepo;
    public RemoverCatalogoCirurgiaCommandHandler(ICatalogoCirurgiaRepository repo, IOrcamentoPacoteRepository pacoteRepo)
    {
        _repo = repo; _pacoteRepo = pacoteRepo;
    }

    public async Task Handle(RemoverCatalogoCirurgiaCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Cirurgia não encontrada.");

        if (await _pacoteRepo.ExistePacoteAtivoComProcedimento(cmd.Id, cmd.EstabelecimentoId))
            throw new BusinessException("Procedimento está em uso por um ou mais pacotes ativos. Desative o pacote primeiro.");

        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

public class CriarValorProfissionalCommandHandler : ICommandHandler<CriarValorProfissionalCommand>
{
    private readonly IValorProfissionalOrcamentoRepository _repo;
    public CriarValorProfissionalCommandHandler(IValorProfissionalOrcamentoRepository repo) => _repo = repo;

    public async Task Handle(CriarValorProfissionalCommand cmd)
    {
        var entity = ValorProfissionalOrcamento.Criar(cmd.EstabelecimentoId, cmd.ProfissionalUsuarioId,
            cmd.Funcao, cmd.TempoBaseMinutos, cmd.ValorTempoBase,
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
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Valor profissional não encontrado.");
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
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Valor profissional não encontrado.");
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

public class SalvarConfiguracaoLocalCommandHandler : ICommandHandler<SalvarConfiguracaoLocalCommand>
{
    private readonly IConfiguracaoLocalCirurgiaRepository _repo;
    public SalvarConfiguracaoLocalCommandHandler(IConfiguracaoLocalCirurgiaRepository repo) => _repo = repo;

    public async Task Handle(SalvarConfiguracaoLocalCommand cmd)
    {
        if (!Enum.TryParse<TipoLocalCirurgia>(cmd.TipoLocal, ignoreCase: true, out var tipo))
            throw new BusinessException($"Tipo de local cirúrgico '{cmd.TipoLocal}' inválido.");

        var entity = await _repo.ObterPorEstabelecimentoETipo(cmd.EstabelecimentoId, tipo);
        if (entity is null)
            entity = ConfiguracaoLocalCirurgia.Criar(cmd.EstabelecimentoId, tipo,
                cmd.TempoBaseMinutos, cmd.ValorBase, cmd.TempoAdicionalMinutos, cmd.ValorAdicional);
        else
            entity.Atualizar(cmd.TempoBaseMinutos, cmd.ValorBase, cmd.TempoAdicionalMinutos, cmd.ValorAdicional);
        await _repo.Salvar(entity);
        cmd.IdSalvo = entity.Id;
    }
}

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
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Equipe não encontrada.");
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
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Equipe não encontrada.");
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

public class CriarCatalogoImplanteCommandHandler : ICommandHandler<CriarCatalogoImplanteCommand>
{
    private readonly ICatalogoImplanteRepository _repo;
    private readonly IItemInventarioRepository _inv;
    public CriarCatalogoImplanteCommandHandler(ICatalogoImplanteRepository repo, IItemInventarioRepository inv)
    { _repo = repo; _inv = inv; }

    public async Task Handle(CriarCatalogoImplanteCommand cmd)
    {
        if (cmd.ItemInventarioId is { } invId)
            _ = await _inv.ObterPorIdOuNulo(invId, cmd.EstabelecimentoId)
                ?? throw new BusinessException($"Item de inventário {invId} não encontrado.");

        var entity = CatalogoImplante.Criar(cmd.EstabelecimentoId, cmd.ItemInventarioId, cmd.Descricao, cmd.CustoUnitario);
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }
}

public class AtualizarCatalogoImplanteCommandHandler : ICommandHandler<AtualizarCatalogoImplanteCommand>
{
    private readonly ICatalogoImplanteRepository _repo;
    private readonly IItemInventarioRepository _inv;
    public AtualizarCatalogoImplanteCommandHandler(ICatalogoImplanteRepository repo, IItemInventarioRepository inv)
    { _repo = repo; _inv = inv; }

    public async Task Handle(AtualizarCatalogoImplanteCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Implante não encontrado.");
        if (cmd.ItemInventarioId is { } invId)
            _ = await _inv.ObterPorIdOuNulo(invId, cmd.EstabelecimentoId)
                ?? throw new BusinessException($"Item de inventário {invId} não encontrado.");
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
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Implante não encontrado.");
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

public class CriarConfiguracaoPagamentoCommandHandler : ICommandHandler<CriarConfiguracaoPagamentoCommand>
{
    private readonly IConfiguracaoPagamentoCatalogoRepository _repo;
    public CriarConfiguracaoPagamentoCommandHandler(IConfiguracaoPagamentoCatalogoRepository repo) => _repo = repo;

    public async Task Handle(CriarConfiguracaoPagamentoCommand cmd)
    {
        var entity = ConfiguracaoPagamentoCatalogo.Criar(cmd.EstabelecimentoId, cmd.FormaPagamentoId,
            cmd.AcrescimoPercentual, cmd.EntradaPercentualPadrao, cmd.TaxaParcela, cmd.ParcelasMaximas);
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
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Configuração de pagamento não encontrada.");
        entity.Atualizar(cmd.AcrescimoPercentual, cmd.EntradaPercentualPadrao, cmd.TaxaParcela, cmd.ParcelasMaximas);
        await _repo.Salvar(entity);
    }
}

public class RemoverConfiguracaoPagamentoCommandHandler : ICommandHandler<RemoverConfiguracaoPagamentoCommand>
{
    private readonly IConfiguracaoPagamentoCatalogoRepository _repo;
    public RemoverConfiguracaoPagamentoCommandHandler(IConfiguracaoPagamentoCatalogoRepository repo) => _repo = repo;

    public async Task Handle(RemoverConfiguracaoPagamentoCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Configuração de pagamento não encontrada.");
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

public class CriarCatalogoProdutoCommandHandler : ICommandHandler<CriarCatalogoProdutoCommand>
{
    private readonly ICatalogoProdutoRepository _repo;
    private readonly IItemInventarioRepository _inv;

    public CriarCatalogoProdutoCommandHandler(ICatalogoProdutoRepository repo, IItemInventarioRepository inv)
    { _repo = repo; _inv = inv; }

    public async Task Handle(CriarCatalogoProdutoCommand cmd)
    {
        // Valida vínculo com item de inventário (R15/CA92): item deve ser do mesmo tenant.
        if (cmd.ItemInventarioId is { } invId)
            _ = await _inv.ObterPorIdOuNulo(invId, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Não encontrado.");

        var tipo = ParseTipoProduto(cmd.Tipo);
        var entity = CatalogoProduto.Criar(cmd.EstabelecimentoId, cmd.Nome, cmd.Descricao,
            cmd.ValorReferencia, cmd.UsoUnico, tipo, cmd.Marca, cmd.Unidade, cmd.FornecedorNome, cmd.CodigoSku,
            cmd.ItemInventarioId);
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }

    internal static TipoOrcamentoProduto ParseTipoProduto(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            throw new BusinessException("Tipo do produto é obrigatório.");
        if (!Enum.TryParse<TipoOrcamentoProduto>(tipo, ignoreCase: true, out var t) || !Enum.IsDefined(t))
            throw new BusinessException("Tipo de produto inválido.");
        return t;
    }
}

public class AtualizarCatalogoProdutoCommandHandler : ICommandHandler<AtualizarCatalogoProdutoCommand>
{
    private readonly ICatalogoProdutoRepository _repo;
    private readonly IItemInventarioRepository _inv;

    public AtualizarCatalogoProdutoCommandHandler(ICatalogoProdutoRepository repo, IItemInventarioRepository inv)
    { _repo = repo; _inv = inv; }

    public async Task Handle(AtualizarCatalogoProdutoCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Produto não encontrado.");

        // Valida vínculo com item de inventário (R15/CA92): item deve ser do mesmo tenant.
        if (cmd.ItemInventarioId is { } invId)
            _ = await _inv.ObterPorIdOuNulo(invId, cmd.EstabelecimentoId)
                ?? throw new BusinessException("Não encontrado.");

        var tipo = CriarCatalogoProdutoCommandHandler.ParseTipoProduto(cmd.Tipo);
        entity.Atualizar(cmd.Nome, cmd.Descricao, cmd.ValorReferencia, cmd.UsoUnico,
            tipo, cmd.Marca, cmd.Unidade, cmd.FornecedorNome, cmd.CodigoSku, cmd.ItemInventarioId);
        await _repo.Salvar(entity);
    }
}

public class RemoverCatalogoProdutoCommandHandler : ICommandHandler<RemoverCatalogoProdutoCommand>
{
    private readonly ICatalogoProdutoRepository _repo;
    public RemoverCatalogoProdutoCommandHandler(ICatalogoProdutoRepository repo) => _repo = repo;

    public async Task Handle(RemoverCatalogoProdutoCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Produto não encontrado.");
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

public class VincularProdutoCirurgiaCommandHandler : ICommandHandler<VincularProdutoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaRepository _cirRepo;
    private readonly ICatalogoProdutoRepository _prodRepo;
    private readonly ICatalogoCirurgiaProdutoRepository _vincRepo;
    public VincularProdutoCirurgiaCommandHandler(ICatalogoCirurgiaRepository cirRepo,
        ICatalogoProdutoRepository prodRepo, ICatalogoCirurgiaProdutoRepository vincRepo)
    { _cirRepo = cirRepo; _prodRepo = prodRepo; _vincRepo = vincRepo; }

    public async Task Handle(VincularProdutoCirurgiaCommand cmd)
    {
        _ = await _cirRepo.ObterPorIdOuNulo(cmd.CatalogoCirurgiaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Cirurgia não encontrada.");
        _ = await _prodRepo.ObterPorIdOuNulo(cmd.CatalogoProdutoId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Produto não encontrado.");

        var existente = await _vincRepo.ObterPorCirurgiaProduto(cmd.CatalogoCirurgiaId, cmd.CatalogoProdutoId);
        if (existente is not null)
        {
            existente.AtualizarQuantidade(cmd.QuantidadePadrao, cmd.Obrigatorio, cmd.Incluido);
            await _vincRepo.Salvar(existente);
            cmd.IdCriado = existente.Id;
            return;
        }

        var vinc = CatalogoCirurgiaProduto.Criar(cmd.CatalogoCirurgiaId, cmd.CatalogoProdutoId,
            cmd.QuantidadePadrao, cmd.Obrigatorio, cmd.Incluido);
        await _vincRepo.Salvar(vinc);
        cmd.IdCriado = vinc.Id;
    }
}

public class AtualizarVinculoProdutoCirurgiaCommandHandler : ICommandHandler<AtualizarVinculoProdutoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaProdutoRepository _vincRepo;
    private readonly ICatalogoCirurgiaRepository _cirRepo;
    public AtualizarVinculoProdutoCirurgiaCommandHandler(ICatalogoCirurgiaProdutoRepository vincRepo,
        ICatalogoCirurgiaRepository cirRepo)
    { _vincRepo = vincRepo; _cirRepo = cirRepo; }

    public async Task Handle(AtualizarVinculoProdutoCirurgiaCommand cmd)
    {
        var vinc = await _vincRepo.ObterPorIdOuNulo(cmd.VinculoId)
            ?? throw new BusinessException("Vínculo não encontrado.");
        _ = await _cirRepo.ObterPorIdOuNulo(vinc.CatalogoCirurgiaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Vínculo não encontrado.");
        vinc.AtualizarQuantidade(cmd.QuantidadePadrao, cmd.Obrigatorio, cmd.Incluido);
        await _vincRepo.Salvar(vinc);
    }
}

public class DesvincularProdutoCirurgiaCommandHandler : ICommandHandler<DesvincularProdutoCirurgiaCommand>
{
    private readonly ICatalogoCirurgiaProdutoRepository _vincRepo;
    private readonly ICatalogoCirurgiaRepository _cirRepo;
    public DesvincularProdutoCirurgiaCommandHandler(ICatalogoCirurgiaProdutoRepository vincRepo,
        ICatalogoCirurgiaRepository cirRepo)
    { _vincRepo = vincRepo; _cirRepo = cirRepo; }

    public async Task Handle(DesvincularProdutoCirurgiaCommand cmd)
    {
        var vinc = await _vincRepo.ObterPorIdOuNulo(cmd.VinculoId)
            ?? throw new BusinessException("Vínculo não encontrado.");
        _ = await _cirRepo.ObterPorIdOuNulo(vinc.CatalogoCirurgiaId, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Vínculo não encontrado.");
        await _vincRepo.Remover(vinc);
    }
}

public class CriarOrcamentoTeamRoleCommandHandler : ICommandHandler<CriarOrcamentoTeamRoleCommand>
{
    private readonly IOrcamentoTeamRoleRepository _repo;
    public CriarOrcamentoTeamRoleCommandHandler(IOrcamentoTeamRoleRepository repo) => _repo = repo;

    public async Task Handle(CriarOrcamentoTeamRoleCommand cmd)
    {
        var tipo = ParseTipoHonorario(cmd.TipoHonorario);
        var entity = OrcamentoTeamRole.Criar(cmd.EstabelecimentoId, cmd.Papel, cmd.ProfissionalUsuarioId,
            cmd.NomePadrao, tipo, cmd.Valor, cmd.BaseCalculo);
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }

    internal static TipoHonorarioTeamRole ParseTipoHonorario(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo)) return TipoHonorarioTeamRole.Percentual;
        if (Enum.TryParse<TipoHonorarioTeamRole>(tipo, ignoreCase: true, out var t)) return t;
        throw new BusinessException("Tipo de honorário inválido.");
    }
}

public class AtualizarOrcamentoTeamRoleCommandHandler : ICommandHandler<AtualizarOrcamentoTeamRoleCommand>
{
    private readonly IOrcamentoTeamRoleRepository _repo;
    public AtualizarOrcamentoTeamRoleCommandHandler(IOrcamentoTeamRoleRepository repo) => _repo = repo;

    public async Task Handle(AtualizarOrcamentoTeamRoleCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Papel de equipe não encontrado.");
        var tipo = CriarOrcamentoTeamRoleCommandHandler.ParseTipoHonorario(cmd.TipoHonorario);
        entity.Atualizar(cmd.Papel, cmd.ProfissionalUsuarioId, cmd.NomePadrao, tipo, cmd.Valor, cmd.BaseCalculo);
        await _repo.Salvar(entity);
    }
}

public class RemoverOrcamentoTeamRoleCommandHandler : ICommandHandler<RemoverOrcamentoTeamRoleCommand>
{
    private readonly IOrcamentoTeamRoleRepository _repo;
    public RemoverOrcamentoTeamRoleCommandHandler(IOrcamentoTeamRoleRepository repo) => _repo = repo;

    public async Task Handle(RemoverOrcamentoTeamRoleCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Papel de equipe não encontrado.");
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

public class CriarOrcamentoAnestesistaCommandHandler : ICommandHandler<CriarOrcamentoAnestesistaCommand>
{
    private readonly IOrcamentoAnestesistaRepository _repo;
    public CriarOrcamentoAnestesistaCommandHandler(IOrcamentoAnestesistaRepository repo) => _repo = repo;

    public async Task Handle(CriarOrcamentoAnestesistaCommand cmd)
    {
        var entity = OrcamentoAnestesista.Criar(cmd.EstabelecimentoId, cmd.Nome, cmd.ProfissionalUsuarioId,
            cmd.Crm, cmd.Especialidade, cmd.Telefone, cmd.TabelaHonorarios);
        entity.SincronizarFaixas(cmd.Faixas.Select(f => (f.Descricao, f.Valor)));
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }
}

public class AtualizarOrcamentoAnestesistaCommandHandler : ICommandHandler<AtualizarOrcamentoAnestesistaCommand>
{
    private readonly IOrcamentoAnestesistaRepository _repo;
    public AtualizarOrcamentoAnestesistaCommandHandler(IOrcamentoAnestesistaRepository repo) => _repo = repo;

    public async Task Handle(AtualizarOrcamentoAnestesistaCommand cmd)
    {
        var entity = await _repo.ObterComFaixasOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Anestesista não encontrado.");
        entity.Atualizar(cmd.Nome, cmd.ProfissionalUsuarioId, cmd.Crm, cmd.Especialidade, cmd.Telefone, cmd.TabelaHonorarios);
        entity.SincronizarFaixas(cmd.Faixas.Select(f => (f.Descricao, f.Valor)));
        await _repo.Salvar(entity);
    }
}

public class RemoverOrcamentoAnestesistaCommandHandler : ICommandHandler<RemoverOrcamentoAnestesistaCommand>
{
    private readonly IOrcamentoAnestesistaRepository _repo;
    public RemoverOrcamentoAnestesistaCommandHandler(IOrcamentoAnestesistaRepository repo) => _repo = repo;

    public async Task Handle(RemoverOrcamentoAnestesistaCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Anestesista não encontrado.");
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}

public class CriarOrcamentoPacoteCommandHandler : ICommandHandler<CriarOrcamentoPacoteCommand>
{
    private readonly IOrcamentoPacoteRepository _repo;
    private readonly ICatalogoCirurgiaRepository _cirRepo;
    private readonly ICatalogoProdutoRepository _prodRepo;
    private readonly IOrcamentoTeamRoleRepository _teamRepo;
    private readonly IOrcamentoAnestesistaRepository _anestRepo;

    public CriarOrcamentoPacoteCommandHandler(IOrcamentoPacoteRepository repo,
        ICatalogoCirurgiaRepository cirRepo, ICatalogoProdutoRepository prodRepo,
        IOrcamentoTeamRoleRepository teamRepo, IOrcamentoAnestesistaRepository anestRepo)
    {
        _repo = repo; _cirRepo = cirRepo; _prodRepo = prodRepo;
        _teamRepo = teamRepo; _anestRepo = anestRepo;
    }

    public async Task Handle(CriarOrcamentoPacoteCommand cmd)
    {
        await ValidarReferencias(cmd.EstabelecimentoId, cmd.AnestesistaId,
            cmd.ProcedimentoIds, cmd.Produtos, cmd.TeamRoleIds,
            _cirRepo, _prodRepo, _teamRepo, _anestRepo);

        var entity = OrcamentoPacote.Criar(cmd.EstabelecimentoId, cmd.Nome, cmd.Descricao,
            cmd.AnestesistaId, cmd.ValorTotalSugerido);
        entity.Sincronizar(cmd.ProcedimentoIds,
            cmd.Produtos.Select(p => (p.ProdutoId, p.Quantidade)),
            cmd.TeamRoleIds);
        await _repo.Salvar(entity);
        cmd.IdCriado = entity.Id;
    }

    internal static async Task ValidarReferencias(long estabelecimentoId, long? anestesistaId,
        IEnumerable<long> procedimentoIds, IEnumerable<ProdutoDoPacoteInput> produtos,
        IEnumerable<long> teamRoleIds,
        ICatalogoCirurgiaRepository cirRepo, ICatalogoProdutoRepository prodRepo,
        IOrcamentoTeamRoleRepository teamRepo, IOrcamentoAnestesistaRepository anestRepo)
    {
        foreach (var pid in procedimentoIds.Distinct())
            _ = await cirRepo.ObterPorIdOuNulo(pid, estabelecimentoId)
                ?? throw new BusinessException("Procedimento não encontrado.");
        foreach (var p in produtos)
            _ = await prodRepo.ObterPorIdOuNulo(p.ProdutoId, estabelecimentoId)
                ?? throw new BusinessException("Produto não encontrado.");
        foreach (var rid in teamRoleIds.Distinct())
            _ = await teamRepo.ObterPorIdOuNulo(rid, estabelecimentoId)
                ?? throw new BusinessException("Papel de equipe não encontrado.");
        if (anestesistaId is { } aid)
            _ = await anestRepo.ObterPorIdOuNulo(aid, estabelecimentoId)
                ?? throw new BusinessException("Anestesista não encontrado.");
    }
}

public class AtualizarOrcamentoPacoteCommandHandler : ICommandHandler<AtualizarOrcamentoPacoteCommand>
{
    private readonly IOrcamentoPacoteRepository _repo;
    private readonly ICatalogoCirurgiaRepository _cirRepo;
    private readonly ICatalogoProdutoRepository _prodRepo;
    private readonly IOrcamentoTeamRoleRepository _teamRepo;
    private readonly IOrcamentoAnestesistaRepository _anestRepo;

    public AtualizarOrcamentoPacoteCommandHandler(IOrcamentoPacoteRepository repo,
        ICatalogoCirurgiaRepository cirRepo, ICatalogoProdutoRepository prodRepo,
        IOrcamentoTeamRoleRepository teamRepo, IOrcamentoAnestesistaRepository anestRepo)
    {
        _repo = repo; _cirRepo = cirRepo; _prodRepo = prodRepo;
        _teamRepo = teamRepo; _anestRepo = anestRepo;
    }

    public async Task Handle(AtualizarOrcamentoPacoteCommand cmd)
    {
        var entity = await _repo.ObterComAssociacoesOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Pacote não encontrado.");

        await CriarOrcamentoPacoteCommandHandler.ValidarReferencias(cmd.EstabelecimentoId, cmd.AnestesistaId,
            cmd.ProcedimentoIds, cmd.Produtos, cmd.TeamRoleIds,
            _cirRepo, _prodRepo, _teamRepo, _anestRepo);

        entity.Atualizar(cmd.Nome, cmd.Descricao, cmd.AnestesistaId, cmd.ValorTotalSugerido);
        entity.Sincronizar(cmd.ProcedimentoIds,
            cmd.Produtos.Select(p => (p.ProdutoId, p.Quantidade)),
            cmd.TeamRoleIds);
        await _repo.Salvar(entity);
    }
}

public class RemoverOrcamentoPacoteCommandHandler : ICommandHandler<RemoverOrcamentoPacoteCommand>
{
    private readonly IOrcamentoPacoteRepository _repo;
    public RemoverOrcamentoPacoteCommandHandler(IOrcamentoPacoteRepository repo) => _repo = repo;

    public async Task Handle(RemoverOrcamentoPacoteCommand cmd)
    {
        var entity = await _repo.ObterPorIdOuNulo(cmd.Id, cmd.EstabelecimentoId)
            ?? throw new BusinessException("Pacote não encontrado.");
        entity.Inativar();
        await _repo.Salvar(entity);
    }
}
