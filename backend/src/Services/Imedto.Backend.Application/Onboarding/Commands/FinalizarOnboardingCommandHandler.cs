using Imedto.Backend.Contracts.Onboarding.Commands;
using Imedto.Backend.Domain.Estabelecimentos;
using Imedto.Backend.Domain.Profissionais;
using Imedto.Backend.Domain.Usuarios;
using Imedto.Backend.Infrastructure.Database.Repositories;
using Imedto.Backend.SharedKernel.Cqrs;
using Imedto.Backend.SharedKernel.Domain;
using Imedto.Backend.SharedKernel.Tenancy;

namespace Imedto.Backend.Application.Onboarding.Commands;

/// <summary>
/// Finaliza o onboarding em uma única transação atômica.
/// A flag <c>onboarding_completo</c> só é definida como <c>true</c> APÓS todos os
/// dados (perfil, estabelecimento, profissional, horários) serem salvos com sucesso.
/// </summary>
public class FinalizarOnboardingCommandHandler : ICommandHandler<FinalizarOnboardingCommand>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IEventBus _eventBus;
    private readonly ICurrentTenantAccessor _tenant;
    private readonly CatalogoQueryRepository _catalogoRepo;

    public FinalizarOnboardingCommandHandler(
        IUsuarioRepository usuarioRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalRepository profissionalRepository,
        IEventBus eventBus,
        ICurrentTenantAccessor tenant,
        CatalogoQueryRepository catalogoRepo)
    {
        _usuarioRepository = usuarioRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
        _eventBus = eventBus;
        _tenant = tenant;
        _catalogoRepo = catalogoRepo;
    }

    public async Task Handle(FinalizarOnboardingCommand command)
    {
        // Defense-in-depth: usa sub do JWT, ignora qualquer Id que viesse no body.
        var usuarioId = _tenant.UsuarioId;
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário não autenticado.");

        var usuario = await _usuarioRepository.ObterPorIdOuNulo(usuarioId)
            ?? throw new BusinessException("Usuário não encontrado.");

        if (usuario.OnboardingCompleto)
            throw new BusinessException("Onboarding já foi concluído.");

        // Valida CPF único antes de salvar qualquer coisa.
        var cpfDigitos = new string((command.Cpf ?? "").Where(char.IsDigit).ToArray());
        if (await _usuarioRepository.ExisteCpf(cpfDigitos, usuarioId))
            throw new BusinessException("CPF já cadastrado em outra conta.");

        // 1. Preenche perfil do usuário (sem marcar onboarding completo ainda).
        usuario.PreencherPerfil(command.NomeCompleto, command.Cpf, command.Telefone);
        await _usuarioRepository.Salvar(usuario);

        long? estabelecimentoId = null;

        // 2. Cria estabelecimento (apenas se for dono).
        if (command.Estabelecimento is not null)
        {
            if (await _estabelecimentoRepository.UsuarioJaEhDono(usuarioId))
                throw new BusinessException("Você já é dono de um estabelecimento.");

            var cnpjDigitos = new string((command.Estabelecimento.Cnpj ?? "").Where(char.IsDigit).ToArray());
            if (cnpjDigitos.Length > 0 && await _estabelecimentoRepository.ExisteCnpj(cnpjDigitos, ignorarEstabelecimentoId: 0))
                throw new BusinessException("CNPJ já cadastrado em outro estabelecimento.");

            var estab = Estabelecimento.Criar(
                usuarioId,
                command.Estabelecimento.NomeFantasia,
                razaoSocial: null,
                command.Estabelecimento.Cnpj,
                command.Estabelecimento.Telefone,
                command.Estabelecimento.Endereco);

            await _estabelecimentoRepository.Salvar(estab); // flush para popular Id
            estab.MarcarComoCriado();

            foreach (var ev in estab.DomainEvents)
                await _eventBus.Publish(ev);
            estab.ClearDomainEvents();

            estabelecimentoId = estab.Id;
        }

        // 3. Salva perfil profissional (se conselho e número de registro foram informados).
        if (command.Profissional is not null
            && !string.IsNullOrWhiteSpace(command.Profissional.NumeroRegistro))
        {
            var conselho = (command.Profissional.Conselho ?? "").Trim().ToUpperInvariant();
            var uf = (command.Profissional.Uf ?? "").Trim().ToUpperInvariant();
            var numero = (command.Profissional.NumeroRegistro ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(conselho) && !string.IsNullOrWhiteSpace(uf) && !string.IsNullOrWhiteSpace(numero)
                && await _profissionalRepository.ExisteConselhoRegistro(conselho, uf, numero, usuarioId))
            {
                throw new BusinessException("Já existe outro profissional com este número de registro neste conselho/UF.");
            }

            var especialidadeFinal = await ResolverEspecialidadesAsync(command.Profissional);

            var existente = await _profissionalRepository.ObterPorIdOuNulo(usuarioId);
            if (existente is null)
            {
                var prof = Profissional.Cadastrar(
                    usuarioId,
                    command.Profissional.Conselho,
                    command.Profissional.Uf,
                    numero,
                    especialidadeFinal,
                    bio: null);

                await _profissionalRepository.Salvar(prof);

                foreach (var ev in prof.DomainEvents)
                    await _eventBus.Publish(ev);
                prof.ClearDomainEvents();
            }
        }

        // 4. Atualiza horários do estabelecimento (apenas se foi criado e dados foram informados).
        if (estabelecimentoId.HasValue && command.Funcionamento is not null && command.Funcionamento.DiasSemana?.Count > 0)
        {
            var estab = await _estabelecimentoRepository.ObterPorId(estabelecimentoId.Value);

            estab.AtualizarFuncionamento(
                TimeOnly.Parse(command.Funcionamento.HorarioInicio),
                TimeOnly.Parse(command.Funcionamento.HorarioFim),
                command.Funcionamento.DuracaoConsultaPadraoMinutos,
                command.Funcionamento.IntervaloEntreConsultasMinutos,
                command.Funcionamento.DiasSemana,
                horariosBloqueados: [],
                datasBloqueadas: []);

            await _estabelecimentoRepository.Salvar(estab);
        }

        // 5. Marca onboarding completo APENAS após todos os dados terem sido salvos.
        usuario.MarcarOnboardingCompleto();
        await _usuarioRepository.Salvar(usuario);
    }

    /// <summary>
    /// Quando o front envia <c>Especialidades</c> (lista do catálogo), valida cada item contra
    /// a profissão informada e devolve uma string CSV. Caso contrário, faz fallback para o
    /// campo legado <c>Especialidade</c> (texto livre — compat com chamadas antigas).
    /// </summary>
    private async Task<string?> ResolverEspecialidadesAsync(ProfissionalOnboardingInput input)
    {
        var lista = (input.Especialidades ?? Array.Empty<string>())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (lista.Count == 0)
            return input.Especialidade;

        if (input.ProfissaoId is not { } profId || profId <= 0)
            throw new BusinessException("Profissão é obrigatória quando especialidades forem informadas.");

        if (!await _catalogoRepo.ExisteProfissaoAtiva(profId))
            throw new BusinessException("Profissão informada é inválida ou está inativa.");

        foreach (var nome in lista)
        {
            if (!await _catalogoRepo.ExisteEspecialidadeAtivaPorNome(profId, nome))
                throw new BusinessException($"Especialidade '{nome}' não pertence à profissão selecionada ou está inativa.");
        }

        return string.Join(", ", lista);
    }
}
