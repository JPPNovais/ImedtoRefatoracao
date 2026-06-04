using Imedto.Backend.Domain.Vinculos.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Vinculos;

/// <summary>
/// Vínculo de um profissional (usuário) a um estabelecimento, com um modelo de permissão aplicado.
/// Fluxo: Convidado → Ativo (profissional aceita) → Inativo (dono ou profissional encerra).
/// </summary>
public class VinculoProfissionalEstabelecimento : Entity
{
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    /// <summary>
    /// Nullable: convite pode ser criado sem modelo de permissão. Enquanto null,
    /// o profissional fica sem acesso (TenantAccessResolver retorna SemAcesso) até
    /// alguém atribuir um modelo via <see cref="AtualizarModeloPermissao"/>.
    /// </summary>
    public virtual long? ModeloPermissaoId { get; protected set; }
    public virtual Guid ConvidadoPorUsuarioId { get; protected set; }
    public virtual VinculoStatus Status { get; protected set; }
    public virtual DateTime ConvidadoEm { get; protected set; }
    public virtual DateTime? AceitoEm { get; protected set; }
    public virtual DateTime? InativadoEm { get; protected set; }

    /// <summary>
    /// Dados que o convidador pode pré-cadastrar para ajudar o convidado no onboarding.
    /// Todos opcionais — quando não preenchidos, o convidado digita do zero.
    /// </summary>
    public virtual string NomeConvidado { get; protected set; }
    public virtual string TelefoneConvidado { get; protected set; }
    public virtual string EspecialidadeConvidada { get; protected set; }
    public virtual long? ProfissaoConvidadaId { get; protected set; }

    protected VinculoProfissionalEstabelecimento() { }

    public static VinculoProfissionalEstabelecimento Convidar(
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        long? modeloPermissaoId,
        Guid convidadoPorUsuarioId,
        string nomeConvidado = null,
        string telefoneConvidado = null,
        string especialidadeConvidada = null,
        long? profissaoConvidadaId = null)
    {
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (modeloPermissaoId is { } id && id <= 0)
            throw new BusinessException("Modelo de permissão é obrigatório.");
        if (convidadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário que convida é obrigatório.");
        if (profissionalUsuarioId == convidadoPorUsuarioId)
            throw new BusinessException("Você não pode convidar a si mesmo.");

        return new VinculoProfissionalEstabelecimento
        {
            ProfissionalUsuarioId = profissionalUsuarioId,
            EstabelecimentoId = estabelecimentoId,
            ModeloPermissaoId = modeloPermissaoId,
            ConvidadoPorUsuarioId = convidadoPorUsuarioId,
            Status = VinculoStatus.Convidado,
            ConvidadoEm = DateTime.UtcNow,
            NomeConvidado = NormalizarTexto(nomeConvidado, 200),
            TelefoneConvidado = NormalizarTelefone(telefoneConvidado),
            EspecialidadeConvidada = NormalizarTexto(especialidadeConvidada, 200),
            ProfissaoConvidadaId = profissaoConvidadaId is { } pid && pid > 0 ? pid : null
        };
    }

    private static string NormalizarTexto(string valor, int max)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var trim = valor.Trim();
        if (trim.Length > max)
            throw new BusinessException($"Texto excede o limite de {max} caracteres.");
        return trim;
    }

    private static string NormalizarTelefone(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var digitos = new string(valor.Where(char.IsDigit).ToArray());
        if (digitos.Length == 0) return null;
        if (digitos.Length > 20)
            throw new BusinessException("Telefone excede 20 dígitos.");
        return digitos;
    }

    /// <summary>
    /// Cria um vínculo já <see cref="VinculoStatus.Ativo"/> — usado pelo fluxo de
    /// solicitação inversa, em que a aprovação do dono + a solicitação prévia do
    /// profissional já configuram consentimento bilateral (não há "aceitar" intermediário).
    /// </summary>
    public static VinculoProfissionalEstabelecimento CriarAtivoPorSolicitacao(
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        long modeloPermissaoId,
        Guid aprovadoPorUsuarioId)
    {
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        // Solicitação inversa exige modelo (o dono está aprovando explicitamente).
        if (modeloPermissaoId <= 0)
            throw new BusinessException("Modelo de permissão é obrigatório.");
        if (aprovadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário que aprova é obrigatório.");
        if (profissionalUsuarioId == aprovadoPorUsuarioId)
            throw new BusinessException("Você não pode aprovar sua própria solicitação.");

        var agora = DateTime.UtcNow;
        return new VinculoProfissionalEstabelecimento
        {
            ProfissionalUsuarioId = profissionalUsuarioId,
            EstabelecimentoId = estabelecimentoId,
            ModeloPermissaoId = modeloPermissaoId,
            ConvidadoPorUsuarioId = aprovadoPorUsuarioId,
            Status = VinculoStatus.Ativo,
            ConvidadoEm = agora,
            AceitoEm = agora
        };
    }

    /// <summary>
    /// Anexa <see cref="ProfissionalConvidadoEvent"/> — chamar após persistir o aggregate.
    /// <paramref name="mensagemPersonalizada"/> transita apenas para o e-mail; não é persistida.
    /// </summary>
    public virtual void MarcarComoConvidado(string? mensagemPersonalizada = null)
    {
        if (Id == 0)
            throw new InvalidOperationException("Vínculo ainda não foi persistido — Id é 0.");

        AddDomainEvent(new ProfissionalConvidadoEvent(
            Id, ProfissionalUsuarioId, EstabelecimentoId, ConvidadoPorUsuarioId,
            mensagemPersonalizada));
    }

    public virtual void Aceitar()
    {
        if (Status != VinculoStatus.Convidado)
            throw new BusinessException("Apenas convites pendentes podem ser aceitos.");

        Status = VinculoStatus.Ativo;
        AceitoEm = DateTime.UtcNow;

        AddDomainEvent(new VinculoAceitoEvent(Id, ProfissionalUsuarioId, EstabelecimentoId));
    }

    public virtual void Inativar()
    {
        if (Status == VinculoStatus.Inativo)
            throw new BusinessException("Vínculo já está inativo.");

        Status = VinculoStatus.Inativo;
        InativadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Reativa um vínculo previamente aceito (Inativo → Ativo). Restaura acesso
    /// imediato, sem novo convite. Para vínculos inativos que nunca chegaram a
    /// ser aceitos (AceitoEm == null), use <see cref="ReativarComoConvite"/>.
    /// </summary>
    public virtual void Reativar()
    {
        if (Status != VinculoStatus.Inativo)
            throw new BusinessException("Apenas vínculos inativos podem ser reativados.");
        if (AceitoEm is null)
            throw new BusinessException("Este vínculo nunca foi aceito — reenvie o convite em vez de reativar.");

        Status = VinculoStatus.Ativo;
        InativadoEm = null;
    }

    /// <summary>
    /// Define (ou limpa) a especialidade do vínculo para este estabelecimento.
    /// String vazia ou nula → null (limpa o campo, fazendo cair no fallback global).
    /// Normaliza e limita a 200 caracteres (mesmo padrão do convite).
    /// Independe do status do vínculo — Convidado/Ativo/Inativo podem ter especialidade definida.
    /// </summary>
    public virtual void AtualizarEspecialidade(string especialidade)
    {
        EspecialidadeConvidada = NormalizarTexto(especialidade, 200);
    }

    /// <summary>
    /// Altera atomicamente a profissão e a especialidade do vínculo neste estabelecimento.
    /// Trocar a profissão sempre limpa a especialidade — não há janela de estado inconsistente.
    /// Profissao nula limpa profissao_convidada_id; especialidade nula/vazia limpa especialidade_convidada.
    /// Independe do status do vínculo.
    /// </summary>
    public virtual void AtualizarProfissaoEspecialidade(long? profissaoId, string? especialidade)
    {
        ProfissaoConvidadaId = profissaoId is { } pid && pid > 0 ? pid : null;
        EspecialidadeConvidada = NormalizarTexto(especialidade, 200);
    }

    public virtual void AtualizarModeloPermissao(long novoModeloPermissaoId)
    {
        if (Status == VinculoStatus.Inativo)
            throw new BusinessException("Não é possível alterar permissões de vínculo inativo.");
        if (novoModeloPermissaoId <= 0)
            throw new BusinessException("Modelo de permissão é obrigatório.");

        var trocou = ModeloPermissaoId != novoModeloPermissaoId;
        ModeloPermissaoId = novoModeloPermissaoId;

        // Só dispara o evento de realtime se o modelo de fato mudou; idempotente.
        // Vínculos em status Convidado também emitem — quando o profissional aceitar
        // e logar, o front já pega a permissão atualizada via bootstrap, e a sessão
        // ativa (caso o convidado já esteja logado em algum cenário) revalida na hora.
        if (trocou && Id != 0)
        {
            AddDomainEvent(new VinculoModeloPermissaoAlteradoEvent(
                Id, ProfissionalUsuarioId, EstabelecimentoId, novoModeloPermissaoId));
        }
    }

    /// <summary>
    /// Reativa um vínculo previamente inativado, transformando-o em novo Convite.
    /// O profissional precisará aceitar de novo, mas o histórico (datas anteriores) é preservado
    /// na linha — apenas <see cref="AceitoEm"/>/<see cref="InativadoEm"/> são zerados.
    /// </summary>
    /// <summary>
    /// Reativa um vínculo previamente inativado, transformando-o em novo Convite.
    /// <paramref name="mensagemPersonalizada"/> transita apenas para o e-mail; não é persistida.
    /// </summary>
    public virtual void ReativarComoConvite(
        long? novoModeloPermissaoId,
        Guid convidadoPorUsuarioId,
        string nomeConvidado = null,
        string telefoneConvidado = null,
        string especialidadeConvidada = null,
        long? profissaoConvidadaId = null,
        string? mensagemPersonalizada = null)
    {
        if (Status != VinculoStatus.Inativo)
            throw new BusinessException("Apenas vínculos inativos podem ser reativados.");
        if (novoModeloPermissaoId is { } id && id <= 0)
            throw new BusinessException("Modelo de permissão é obrigatório.");
        if (convidadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário que convida é obrigatório.");

        Status = VinculoStatus.Convidado;
        ModeloPermissaoId = novoModeloPermissaoId;
        ConvidadoPorUsuarioId = convidadoPorUsuarioId;
        ConvidadoEm = DateTime.UtcNow;
        AceitoEm = null;
        InativadoEm = null;
        NomeConvidado = NormalizarTexto(nomeConvidado, 200);
        TelefoneConvidado = NormalizarTelefone(telefoneConvidado);
        EspecialidadeConvidada = NormalizarTexto(especialidadeConvidada, 200);
        ProfissaoConvidadaId = profissaoConvidadaId is { } pid && pid > 0 ? pid : null;

        AddDomainEvent(new ProfissionalConvidadoEvent(
            Id, ProfissionalUsuarioId, EstabelecimentoId, ConvidadoPorUsuarioId,
            mensagemPersonalizada));
    }
}
