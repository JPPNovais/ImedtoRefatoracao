using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Termos;

/// <summary>
/// Aggregate root — modelo (template) de termo de consentimento. Pode ser:
/// <list type="bullet">
///   <item>Padrão do sistema: <see cref="EstabelecimentoId"/> = <c>null</c>. Imutável pelo
///   tenant — só pode ser clonado, não editado nem desativado pelo dono do estabelecimento.</item>
///   <item>Do estabelecimento: <see cref="EstabelecimentoId"/> preenchido. Pode ter sido
///   criado do zero ou clonado de um padrão (rastreado em <see cref="PadraoClonadoDeId"/>).</item>
/// </list>
///
/// <para>
/// Versão é incrementada APENAS quando o conteúdo HTML muda (mudança de título não bumpa).
/// O histórico imutável vive em <see cref="TermoModeloVersao"/>.
/// </para>
/// <para>
/// Soft-delete via <see cref="DeletadoEm"/> — termos emitidos sempre carregam um snapshot,
/// então a remoção física do modelo quebraria audit trail (LGPD).
/// </para>
/// </summary>
public class TermoModelo : Entity
{
    public virtual long? EstabelecimentoId { get; protected set; }
    public virtual CategoriaTermo Categoria { get; protected set; }
    public virtual string Titulo { get; protected set; }
    public virtual string ConteudoHtml { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual int VersaoAtual { get; protected set; }
    /// <summary>FK opcional para outro <see cref="TermoModelo"/> — preenchido quando este foi clonado de um padrão do sistema.</summary>
    public virtual long? PadraoClonadoDeId { get; protected set; }
    public virtual DateTime? DeletadoEm { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual Guid? CriadoPorUsuarioId { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    /// <summary>Token de concorrência optimista — mapeado para <c>xmin</c> do Postgres.</summary>
    public virtual uint VersaoConcorrencia { get; protected set; }

    public virtual bool EhPadraoDoSistema => EstabelecimentoId is null;
    public virtual bool EstaDeletado => DeletadoEm.HasValue;

    public const int TituloMaximo = 120;
    public const int TituloMinimo = 3;
    /// <summary>200 KB de HTML. Suficiente para termos longos. Defesa contra DoS por blob enorme.</summary>
    public const int ConteudoHtmlMaximoBytes = 200 * 1024;

    protected TermoModelo() { }

    /// <summary>
    /// Cria um modelo de estabelecimento (do zero). Para clonar um padrão do sistema,
    /// usar <see cref="ClonarDePadrao"/>.
    /// </summary>
    public static TermoModelo CriarDoEstabelecimento(
        long estabelecimentoId,
        Guid criadoPorUsuarioId,
        CategoriaTermo categoria,
        string titulo,
        string conteudoHtmlSanitizado)
    {
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (criadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário é obrigatório.");

        ValidarTitulo(titulo);
        ValidarConteudoHtml(conteudoHtmlSanitizado);

        return new TermoModelo
        {
            EstabelecimentoId = estabelecimentoId,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            Categoria = categoria,
            Titulo = titulo.Trim(),
            ConteudoHtml = conteudoHtmlSanitizado,
            Ativo = true,
            VersaoAtual = 1,
            CriadoEm = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Cria um padrão do sistema. Não é chamado por handler — é usado apenas pelo seed.
    /// </summary>
    public static TermoModelo CriarPadraoDoSistema(
        CategoriaTermo categoria,
        string titulo,
        string conteudoHtmlSanitizado)
    {
        ValidarTitulo(titulo);
        ValidarConteudoHtml(conteudoHtmlSanitizado);

        return new TermoModelo
        {
            EstabelecimentoId = null,
            CriadoPorUsuarioId = null,
            Categoria = categoria,
            Titulo = titulo.Trim(),
            ConteudoHtml = conteudoHtmlSanitizado,
            Ativo = true,
            VersaoAtual = 1,
            CriadoEm = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Clona um padrão do sistema para o estabelecimento. Mantém categoria, título e
    /// conteúdo iniciais; tenant fica livre para editar depois.
    /// </summary>
    public static TermoModelo ClonarDePadrao(
        TermoModelo padrao,
        long estabelecimentoId,
        Guid criadoPorUsuarioId)
    {
        if (padrao is null)
            throw new BusinessException("Modelo padrão é obrigatório.");
        if (!padrao.EhPadraoDoSistema)
            throw new BusinessException("Apenas modelos padrão do sistema podem ser clonados.");
        if (padrao.EstaDeletado || !padrao.Ativo)
            throw new BusinessException("Modelo padrão indisponível.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (criadoPorUsuarioId == Guid.Empty)
            throw new BusinessException("Usuário é obrigatório.");

        return new TermoModelo
        {
            EstabelecimentoId = estabelecimentoId,
            CriadoPorUsuarioId = criadoPorUsuarioId,
            Categoria = padrao.Categoria,
            Titulo = padrao.Titulo,
            ConteudoHtml = padrao.ConteudoHtml,
            Ativo = true,
            VersaoAtual = 1,
            PadraoClonadoDeId = padrao.Id,
            CriadoEm = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Atualiza título/categoria/conteúdo. Só bumpa <see cref="VersaoAtual"/> se o
    /// HTML mudou — handler usa esse retorno pra decidir se grava
    /// <see cref="TermoModeloVersao"/>.
    /// </summary>
    /// <returns><c>true</c> se a versão foi incrementada.</returns>
    public virtual bool Atualizar(
        CategoriaTermo categoria,
        string titulo,
        string conteudoHtmlSanitizado)
    {
        if (EhPadraoDoSistema)
            throw new BusinessException("Modelos padrão do sistema não podem ser editados — clone primeiro.");
        if (EstaDeletado)
            throw new BusinessException("Modelo deletado.");

        ValidarTitulo(titulo);
        ValidarConteudoHtml(conteudoHtmlSanitizado);

        var conteudoMudou = !string.Equals(NormalizarHtml(ConteudoHtml), NormalizarHtml(conteudoHtmlSanitizado), StringComparison.Ordinal);

        Titulo = titulo.Trim();
        Categoria = categoria;
        if (conteudoMudou)
        {
            ConteudoHtml = conteudoHtmlSanitizado;
            VersaoAtual += 1;
        }
        AtualizadoEm = DateTime.UtcNow;
        return conteudoMudou;
    }

    public virtual void AlterarAtivo(bool ativo)
    {
        if (EhPadraoDoSistema)
            throw new BusinessException("Modelos padrão do sistema não podem ser desativados.");
        if (EstaDeletado)
            throw new BusinessException("Modelo deletado.");
        if (Ativo == ativo) return;

        Ativo = ativo;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void MarcarComoDeletado()
    {
        if (EhPadraoDoSistema)
            throw new BusinessException("Modelos padrão do sistema não podem ser deletados.");
        if (EstaDeletado)
            throw new BusinessException("Modelo já foi deletado.");

        DeletadoEm = DateTime.UtcNow;
        Ativo = false;
    }

    /// <summary>
    /// Gera o snapshot da versão atual — chamado pelo handler antes de salvar
    /// para criar o registro imutável em <see cref="TermoModeloVersao"/>.
    /// </summary>
    public virtual TermoModeloVersao CriarSnapshotVersaoAtual(Guid? autorUsuarioId)
    {
        if (Id == 0)
            throw new InvalidOperationException("Modelo ainda não foi persistido — Id é 0.");
        return TermoModeloVersao.Registrar(Id, VersaoAtual, ConteudoHtml, autorUsuarioId);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static void ValidarTitulo(string titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new BusinessException("Título é obrigatório.");
        var t = titulo.Trim();
        if (t.Length < TituloMinimo)
            throw new BusinessException($"Título deve ter pelo menos {TituloMinimo} caracteres.");
        if (t.Length > TituloMaximo)
            throw new BusinessException($"Título excede {TituloMaximo} caracteres.");
    }

    private static void ValidarConteudoHtml(string conteudoHtml)
    {
        if (string.IsNullOrWhiteSpace(conteudoHtml))
            throw new BusinessException("Conteúdo é obrigatório.");
        // Tamanho em bytes UTF-8 — defesa contra blob enorme. A sanitização é responsabilidade
        // do caller (ITermoHtmlSanitizer no Application layer).
        var bytes = System.Text.Encoding.UTF8.GetByteCount(conteudoHtml);
        if (bytes > ConteudoHtmlMaximoBytes)
            throw new BusinessException($"Conteúdo excede o limite de {ConteudoHtmlMaximoBytes / 1024} KB.");
    }

    /// <summary>
    /// Normaliza HTML pra comparação de "mudou ou não": colapsa CRLF→LF e remove
    /// whitespace nas pontas. Não tenta entender HTML — só evita bumpar versão por
    /// diferença de quebra de linha.
    /// </summary>
    private static string NormalizarHtml(string html) =>
        html?.Replace("\r\n", "\n").Trim() ?? string.Empty;
}
