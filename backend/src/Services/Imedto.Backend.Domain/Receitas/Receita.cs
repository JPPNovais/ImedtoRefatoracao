using Imedto.Backend.Domain.Receitas.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Receitas;

/// <summary>
/// Aggregate root — receita médica (prescrição) emitida por um profissional para
/// um paciente em um estabelecimento. Container de <see cref="ItemReceita"/>.
///
/// Estados:
/// - <see cref="StatusReceita.Rascunho"/>: em construção; aceita autosave de
///   observações/itens. Não loga audit de Escrita até virar <see cref="StatusReceita.Emitida"/>.
/// - <see cref="StatusReceita.Emitida"/>: receita finalizada. <c>EmitidaEm</c>
///   passa a ter valor.
/// - <see cref="StatusReceita.Cancelada"/>: profissional registrou correção
///   (paciente vê erro). Visível no histórico.
/// - <see cref="StatusReceita.Substituida"/>: foi gerada uma nova receita que
///   a substitui (ex.: troca de dosagem). Mantém o vínculo via aggregate de
///   destino.
///
/// Implementa <see cref="ISoftDeletable"/> — soft delete é separado de
/// "Cancelada": cancelar é estado clínico (auditável); deletar é remoção lógica
/// pelo dono do registro (some da UI mas o blob permanece).
/// </summary>
public class Receita : Entity, ISoftDeletable
{
    public virtual long ProntuarioId { get; protected set; }
    public virtual long PacienteId { get; protected set; }
    public virtual Guid ProfissionalUsuarioId { get; protected set; }
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual TipoReceita Tipo { get; protected set; }
    /// <summary>
    /// Tipo da notificação (Portaria 344/98) para receitas controladas.
    /// Sempre <c>null</c> quando <see cref="Tipo"/> != <see cref="TipoReceita.Controlada"/>;
    /// obrigatório quando ==.
    /// </summary>
    public virtual TipoNotificacao? TipoNotificacao { get; protected set; }
    /// <summary>Data/hora da emissão clínica. <c>null</c> em rascunho.</summary>
    public virtual DateTime? EmitidaEm { get; protected set; }
    public virtual DateTime? ValidadeAte { get; protected set; }
    /// <summary>
    /// Indica que a receita deve ser <b>retida</b> pela farmácia (não devolvida
    /// ao paciente). Setado automaticamente conforme regras ANVISA (Controlada —
    /// Portaria 344/98 — e Antibiótico — RDC 471/2021). Front usa para exibir
    /// badge "RETER" no PDF/visualização.
    /// </summary>
    public virtual bool RequerRetencao { get; protected set; }
    public virtual string? Observacoes { get; protected set; }
    public virtual StatusReceita Status { get; protected set; }
    /// <summary>
    /// Estado da assinatura digital ICP-Brasil. Máquina de estados:
    /// NaoAssinada → AssinaturaPendente → AssinadaIcp | FalhaAssinatura | AssinaturaExpirada.
    /// Coluna no banco: <c>assinatura_digital_status</c> (varchar 20).
    /// </summary>
    public virtual StatusAssinaturaDigital AssinaturaDigitalStatus { get; protected set; }
    /// <summary>S3 key do PDF assinado (PAdES AD_RB). Null enquanto não assinado.</summary>
    public virtual string? PdfAssinadoS3Key { get; protected set; }
    /// <summary>Quando foi disparada a assinatura no provedor ICP-Brasil.</summary>
    public virtual DateTime? AssinaturaSolicitadaEm { get; protected set; }
    /// <summary>Quando o provedor confirmou a assinatura com sucesso.</summary>
    public virtual DateTime? AssinadaEm { get; protected set; }
    public virtual DateTime? CanceladaEm { get; protected set; }
    public virtual string? MotivoCancelamento { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    // Soft delete — LGPD: prescrição é dado clínico, append-only.
    public virtual DateTime? DeletadoEm { get; protected set; }
    public virtual Guid? DeletadoPorUsuarioId { get; protected set; }

    public virtual List<ItemReceita> Itens { get; protected set; } = new();

    protected Receita() { }

    /// <summary>
    /// Tupla "curta" (5 campos) usada por fluxos legados/handlers que não conhecem
    /// concentração/forma/duração. Mantida como atalho — converte para a estrutura
    /// rica antes de passar para a fábrica.
    /// </summary>
    public record ItemReceitaInput(
        string Medicamento,
        string Posologia,
        string? Quantidade,
        ViaAdministracao? Via,
        string? Observacao,
        string? Concentracao = null,
        string? FormaFarmaceutica = null,
        string? Duracao = null);

    /// <summary>
    /// Fábrica de emissão direta (atalho — sem passar por rascunho). Cria já em
    /// status <see cref="StatusReceita.Emitida"/> com <c>EmitidaEm = now</c>.
    /// Valida regra clínica (≥1 item, controlada exige validade futura + tipo
    /// de notificação) e monta os <see cref="ItemReceita"/> com ordem sequencial.
    /// </summary>
    public static Receita Emitir(
        long prontuarioId,
        long pacienteId,
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        TipoReceita tipo,
        string? observacoes,
        DateTime? validadeAte,
        IEnumerable<(string medicamento, string posologia, string? quantidade, ViaAdministracao? via, string? observacao)> itens,
        TipoNotificacao? tipoNotificacao = null)
    {
        var itensRicos = (itens ?? Enumerable.Empty<(string, string, string?, ViaAdministracao?, string?)>())
            .Select(t => new ItemReceitaInput(t.medicamento, t.posologia, t.quantidade, t.via, t.observacao));

        return Emitir(prontuarioId, pacienteId, profissionalUsuarioId, estabelecimentoId,
            tipo, tipoNotificacao, observacoes, validadeAte, itensRicos);
    }

    /// <summary>
    /// Sobrecarga rica: aceita <see cref="ItemReceitaInput"/> com concentração,
    /// forma farmacêutica e duração. Idêntica em regra à versão "curta".
    /// </summary>
    public static Receita Emitir(
        long prontuarioId,
        long pacienteId,
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        TipoReceita tipo,
        TipoNotificacao? tipoNotificacao,
        string? observacoes,
        DateTime? validadeAte,
        IEnumerable<ItemReceitaInput> itens)
    {
        ValidarBase(prontuarioId, pacienteId, profissionalUsuarioId, estabelecimentoId, observacoes);
        ValidarNotificacao(tipo, tipoNotificacao);

        var lista = itens?.ToList() ?? new List<ItemReceitaInput>();
        if (lista.Count == 0)
            throw new BusinessException("A receita deve ter ao menos um medicamento.");

        var agora = DateTime.UtcNow;
        var validadeFinal = AplicarRegrasAnvisa(tipo, tipoNotificacao, agora, validadeAte);

        var receita = new Receita
        {
            ProntuarioId = prontuarioId,
            PacienteId = pacienteId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            EstabelecimentoId = estabelecimentoId,
            Tipo = tipo,
            TipoNotificacao = tipoNotificacao,
            EmitidaEm = agora,
            ValidadeAte = validadeFinal,
            RequerRetencao = RegrasAnvisa.RequerRetencao(tipo),
            Observacoes = NormalizarObservacoes(observacoes),
            Status = StatusReceita.Emitida,
            // Default explícito — sem integração com ICP/Memed, toda emissão
            // nasce não-assinada digitalmente (front exibe aviso de assinar manualmente).
            AssinaturaDigitalStatus = StatusAssinaturaDigital.NaoAssinada,
            CriadaEm = agora
        };

        for (var i = 0; i < lista.Count; i++)
            receita.Itens.Add(MaterializarItem(i, lista[i]));

        return receita;
    }

    /// <summary>
    /// Inicia uma receita em <see cref="StatusReceita.Rascunho"/>. Não exige
    /// itens (pode iniciar vazia). Não exige validade futura para controlada
    /// neste estágio — a regra é cobrada em <see cref="Finalizar"/>. O tipo de
    /// notificação, se controlada, segue a mesma regra do <see cref="Emitir"/>:
    /// obrigatório quando controlada, proibido caso contrário.
    /// </summary>
    public static Receita IniciarRascunho(
        long prontuarioId,
        long pacienteId,
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        TipoReceita tipo,
        TipoNotificacao? tipoNotificacao,
        string? observacoes,
        DateTime? validadeAte,
        IEnumerable<ItemReceitaInput>? itens = null)
    {
        ValidarBase(prontuarioId, pacienteId, profissionalUsuarioId, estabelecimentoId, observacoes);
        ValidarNotificacao(tipo, tipoNotificacao);

        var agora = DateTime.UtcNow;
        var receita = new Receita
        {
            ProntuarioId = prontuarioId,
            PacienteId = pacienteId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            EstabelecimentoId = estabelecimentoId,
            Tipo = tipo,
            TipoNotificacao = tipoNotificacao,
            EmitidaEm = null,
            ValidadeAte = validadeAte,
            // RequerRetencao depende apenas do Tipo — pode ser definido já em rascunho
            // para que o front saiba como rotular a prévia.
            RequerRetencao = RegrasAnvisa.RequerRetencao(tipo),
            Observacoes = NormalizarObservacoes(observacoes),
            Status = StatusReceita.Rascunho,
            AssinaturaDigitalStatus = StatusAssinaturaDigital.NaoAssinada,
            CriadaEm = agora
        };

        var lista = itens?.ToList() ?? new List<ItemReceitaInput>();
        for (var i = 0; i < lista.Count; i++)
            receita.Itens.Add(MaterializarItem(i, lista[i]));

        return receita;
    }

    /// <summary>
    /// Atualiza um rascunho (autosave). Substitui observações e itens — não há
    /// merge incremental. Disponível apenas em status <see cref="StatusReceita.Rascunho"/>.
    /// </summary>
    public virtual void AtualizarRascunho(string? observacoes, IEnumerable<ItemReceitaInput>? itens)
    {
        if (Status != StatusReceita.Rascunho)
            throw new BusinessException("Apenas rascunhos podem ser atualizados.");
        if (observacoes is not null && observacoes.Length > 2000)
            throw new BusinessException("Observações excedem 2000 caracteres.");

        Observacoes = NormalizarObservacoes(observacoes);

        Itens.Clear();
        var lista = itens?.ToList() ?? new List<ItemReceitaInput>();
        for (var i = 0; i < lista.Count; i++)
            Itens.Add(MaterializarItem(i, lista[i]));

        AtualizadaEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Finaliza um rascunho — vira <see cref="StatusReceita.Emitida"/>. Aplica
    /// as regras clínicas que rascunho não exige: ≥1 item, controlada exige
    /// validade futura.
    /// </summary>
    public virtual void Finalizar()
    {
        if (Status != StatusReceita.Rascunho)
            throw new BusinessException("Apenas rascunhos podem ser finalizados.");
        if (Itens.Count == 0)
            throw new BusinessException("A receita deve ter ao menos um medicamento.");

        var agora = DateTime.UtcNow;
        ValidadeAte = AplicarRegrasAnvisa(Tipo, TipoNotificacao, agora, ValidadeAte);
        RequerRetencao = RegrasAnvisa.RequerRetencao(Tipo);

        Status = StatusReceita.Emitida;
        EmitidaEm = agora;
        AtualizadaEm = agora;
    }

    /// <summary>
    /// Anexa <see cref="ReceitaEmitidaEvent"/> ao aggregate. Chamar APÓS o
    /// primeiro <c>Salvar</c> — o evento carrega o Id, que só é resolvido
    /// pelo banco no insert.
    /// </summary>
    public virtual void MarcarComoEmitida()
    {
        if (Id == 0)
            throw new InvalidOperationException("Receita ainda não foi persistida — Id é 0.");
        AddDomainEvent(new ReceitaEmitidaEvent(
            Id, ProntuarioId, PacienteId, EstabelecimentoId, ProfissionalUsuarioId, Tipo));
    }

    public virtual void Cancelar(string motivo)
    {
        if (Status != StatusReceita.Emitida)
            throw new BusinessException("Apenas receitas emitidas podem ser canceladas.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessException("Motivo do cancelamento é obrigatório.");
        if (motivo.Length > 500)
            throw new BusinessException("Motivo do cancelamento excede 500 caracteres.");

        Status = StatusReceita.Cancelada;
        CanceladaEm = DateTime.UtcNow;
        MotivoCancelamento = motivo.Trim();
        AtualizadaEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca a receita como substituída por uma nova. O id da nova receita é
    /// guardado externamente (via referência semântica em audit/log) — não
    /// criamos uma FK aqui para manter o aggregate enxuto. Se o produto pedir
    /// versionamento real, virar um campo dedicado depois.
    /// </summary>
    public virtual void Substituir(long novaReceitaId)
    {
        if (Status != StatusReceita.Emitida)
            throw new BusinessException("Apenas receitas emitidas podem ser substituídas.");
        if (novaReceitaId <= 0)
            throw new BusinessException("Nova receita inválida.");

        Status = StatusReceita.Substituida;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void MarcarComoDeletado(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new BusinessException("Usuário responsável pela exclusão é obrigatório.");
        if (DeletadoEm is not null)
            throw new BusinessException("Receita já está deletada.");
        DeletadoEm = DateTime.UtcNow;
        DeletadoPorUsuarioId = usuarioId;
    }

    // --------------------- transições de assinatura digital ---------------------

    /// <summary>
    /// Dispara a assinatura digital: transiciona para <see cref="StatusAssinaturaDigital.AssinaturaPendente"/>.
    /// Permitido de: NaoAssinada, FalhaAssinatura, AssinaturaExpirada.
    /// </summary>
    public virtual void IniciarAssinatura()
    {
        if (AssinaturaDigitalStatus == StatusAssinaturaDigital.AssinadaIcp)
            throw new BusinessException("Esta receita já está assinada digitalmente.");

        if (Status != StatusReceita.Emitida)
            throw new BusinessException("Apenas receitas emitidas podem ser assinadas digitalmente.");

        AssinaturaDigitalStatus = StatusAssinaturaDigital.AssinaturaPendente;
        AssinaturaSolicitadaEm = DateTime.UtcNow;
        AtualizadaEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra conclusão bem-sucedida da assinatura via callback do provedor.
    /// </summary>
    public virtual void ConfirmarAssinatura(string pdfAssinadoS3Key)
    {
        if (AssinaturaDigitalStatus != StatusAssinaturaDigital.AssinaturaPendente)
            throw new BusinessException("Receita não está aguardando confirmação de assinatura.");
        if (string.IsNullOrWhiteSpace(pdfAssinadoS3Key))
            throw new BusinessException("S3 key do PDF assinado é obrigatória.");

        AssinaturaDigitalStatus = StatusAssinaturaDigital.AssinadaIcp;
        PdfAssinadoS3Key = pdfAssinadoS3Key;
        AssinadaEm = DateTime.UtcNow;
        AtualizadaEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra falha na assinatura (médico recusou PUSH ou erro do provedor).
    /// </summary>
    public virtual void RegistrarFalhaAssinatura()
    {
        if (AssinaturaDigitalStatus != StatusAssinaturaDigital.AssinaturaPendente)
            return; // Idempotente: se já foi resolvido de outra forma, ignora.

        AssinaturaDigitalStatus = StatusAssinaturaDigital.FalhaAssinatura;
        AtualizadaEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Expiração por job periódico: pendentes sem resposta após X minutos viram AssinaturaExpirada.
    /// Chamado apenas quando status == AssinaturaPendente.
    /// </summary>
    public virtual void ExpirarAssinaturaPendente()
    {
        if (AssinaturaDigitalStatus != StatusAssinaturaDigital.AssinaturaPendente)
            return; // Idempotente.

        AssinaturaDigitalStatus = StatusAssinaturaDigital.AssinaturaExpirada;
        AtualizadaEm = DateTime.UtcNow;
    }

    // ---------------------------- helpers privados ----------------------------

    private static void ValidarBase(
        long prontuarioId,
        long pacienteId,
        Guid profissionalUsuarioId,
        long estabelecimentoId,
        string? observacoes)
    {
        if (prontuarioId <= 0)
            throw new BusinessException("Prontuário é obrigatório.");
        if (pacienteId <= 0)
            throw new BusinessException("Paciente é obrigatório.");
        if (profissionalUsuarioId == Guid.Empty)
            throw new BusinessException("Profissional emissor é obrigatório.");
        if (estabelecimentoId <= 0)
            throw new BusinessException("Estabelecimento é obrigatório.");
        if (observacoes is not null && observacoes.Length > 2000)
            throw new BusinessException("Observações excedem 2000 caracteres.");
    }

    private static void ValidarNotificacao(TipoReceita tipo, TipoNotificacao? tipoNotificacao)
    {
        if (tipo == TipoReceita.Controlada && tipoNotificacao is null)
            throw new BusinessException("Receita controlada exige tipo de notificação (A, B, C ou Especial).");
        if (tipo != TipoReceita.Controlada && tipoNotificacao is not null)
            throw new BusinessException("Tipo de notificação só se aplica a receita controlada.");
    }

    /// <summary>
    /// Aplica as regras ANVISA de prazo de validade na emissão/finalização de
    /// uma receita. Retorna a data de validade efetiva — calculada
    /// automaticamente quando o caller não informou e a regra exige prazo;
    /// validada quando informada.
    ///
    /// <list type="bullet">
    ///   <item><see cref="TipoReceita.Controlada"/>: validade obrigatória,
    ///     futura, e dentro do prazo da classe (Portaria 344/98 — A/B/C: 30 dias;
    ///     Especial: prazo livre).</item>
    ///   <item><see cref="TipoReceita.Antibiotico"/>: validade futura, máx
    ///     10 dias da emissão (RDC 471/2021). Auto-calcula em
    ///     <c>EmitidaEm + 10 dias</c> quando não informado.</item>
    ///   <item><see cref="TipoReceita.Comum"/> / <see cref="TipoReceita.Especial"/>:
    ///     sem regra de prazo — devolve o valor recebido (pode ser null).</item>
    /// </list>
    /// </summary>
    private static DateTime? AplicarRegrasAnvisa(
        TipoReceita tipo,
        TipoNotificacao? tipoNotificacao,
        DateTime emitidaEm,
        DateTime? validadeAte)
    {
        if (tipo == TipoReceita.Controlada)
        {
            if (validadeAte is null)
            {
                // Notificação Especial: prazo livre — não dá pra auto-calcular,
                // o prescritor precisa informar.
                var auto = RegrasAnvisa.CalcularValidadeMaxima(tipo, tipoNotificacao, emitidaEm);
                if (auto is null)
                    throw new BusinessException("Receita controlada exige data de validade.");
                return auto;
            }

            if (validadeAte <= emitidaEm)
                throw new BusinessException("Validade da receita controlada deve ser futura.");

            RegrasAnvisa.ValidarPrazo(tipo, tipoNotificacao, emitidaEm, validadeAte.Value);
            return validadeAte;
        }

        if (tipo == TipoReceita.Antibiotico)
        {
            if (validadeAte is null)
                return RegrasAnvisa.CalcularValidadeMaxima(tipo, tipoNotificacao, emitidaEm);

            if (validadeAte <= emitidaEm)
                throw new BusinessException("Validade da receita de antibiótico deve ser futura.");

            RegrasAnvisa.ValidarPrazo(tipo, tipoNotificacao, emitidaEm, validadeAte.Value);
            return validadeAte;
        }

        // Comum / Especial — sem regra de prazo.
        return validadeAte;
    }

    private static string? NormalizarObservacoes(string? observacoes) =>
        string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();

    private static ItemReceita MaterializarItem(int ordem, ItemReceitaInput input) =>
        ItemReceita.Criar(
            ordem,
            input.Medicamento,
            input.Posologia,
            input.Quantidade,
            input.Via,
            input.Observacao,
            input.Concentracao,
            input.FormaFarmaceutica,
            input.Duracao);
}
