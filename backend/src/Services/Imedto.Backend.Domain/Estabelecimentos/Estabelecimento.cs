using System.Text.Json;
using Imedto.Backend.Domain.Estabelecimentos.Events;
using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Estabelecimentos;

/// <summary>
/// Aggregate root de Estabelecimento — tenant root da plataforma.
/// Todo dado de domínio (pacientes, prontuários, agenda, etc.) vai ter FK para este aggregate.
/// </summary>
public class Estabelecimento : Entity
{
    public virtual Guid DonoUsuarioId { get; protected set; }
    public virtual string NomeFantasia { get; protected set; }
    public virtual string RazaoSocial { get; protected set; }
    public virtual string Cnpj { get; protected set; }
    public virtual string Telefone { get; protected set; }
    public virtual string Endereco { get; protected set; }
    public virtual string FotoUrl { get; protected set; }
    public virtual EstabelecimentoStatus Status { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    // Funcionamento (configuração da agenda).
    public virtual TimeOnly HorarioInicio { get; protected set; }
    public virtual TimeOnly HorarioFim { get; protected set; }

    /// <summary>Duração padrão de uma consulta em minutos. Define o tamanho dos slots na agenda.</summary>
    public virtual int DuracaoConsultaPadraoMinutos { get; protected set; }

    /// <summary>Intervalo (gap) em minutos entre o fim de uma consulta e o início da próxima.</summary>
    public virtual int IntervaloEntreConsultasMinutos { get; protected set; }

    // JSONB persistido — acesso tipado via propriedades read-only abaixo.
    public virtual string DiasSemanaFuncionamentoJson { get; protected set; }
    public virtual string HorariosBloqueadosJson { get; protected set; }
    public virtual string DatasBloqueadasJson { get; protected set; }

    public IReadOnlyList<int> DiasSemanaFuncionamento =>
        DeserializarSeguro<List<int>>(DiasSemanaFuncionamentoJson, _jsonOpts) ?? new();

    public IReadOnlyList<HorarioBloqueado> HorariosBloqueados =>
        DeserializarSeguro<List<HorarioBloqueado>>(HorariosBloqueadosJson, _jsonOpts) ?? new();

    public IReadOnlyList<DataBloqueada> DatasBloqueadas =>
        DeserializarSeguro<List<DataBloqueada>>(DatasBloqueadasJson, _jsonOpts) ?? new();

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Deserializa JSON dos campos de configuração com fail-open: se o JSON estiver
    /// corrompido (formato antigo, schema-drift, valor inválido), retorna lista
    /// vazia em vez de propagar <see cref="JsonException"/>. Sem isso, qualquer
    /// chamada que toque <see cref="ValidarPodeAgendar"/> virava 500 ErroInterno
    /// (achado lateral Parte 8 do qa/REPORT-V2.md). Lista vazia é o estado mais
    /// permissivo — equivalente a "sem bloqueios", e o front oferece tela de
    /// configuração para reconstruir o array com schema correto.
    /// </summary>
    private static T? DeserializarSeguro<T>(string? json, JsonSerializerOptions opts) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json, opts);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    protected Estabelecimento() { }

    public static Estabelecimento Criar(
        Guid donoUsuarioId,
        string nomeFantasia,
        string razaoSocial,
        string cnpj,
        string telefone,
        string endereco)
    {
        if (donoUsuarioId == Guid.Empty)
            throw new BusinessException("Dono do estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(nomeFantasia))
            throw new BusinessException("Nome fantasia é obrigatório.");

        var cnpjDigitos = string.IsNullOrWhiteSpace(cnpj) ? null : SomenteDigitos(cnpj);
        if (cnpjDigitos is { Length: > 0 and not 14 })
            throw new BusinessException("CNPJ deve conter 14 dígitos.");

        return new Estabelecimento
        {
            DonoUsuarioId = donoUsuarioId,
            NomeFantasia = nomeFantasia.Trim(),
            RazaoSocial = string.IsNullOrWhiteSpace(razaoSocial) ? null : razaoSocial.Trim(),
            Cnpj = cnpjDigitos,
            Telefone = string.IsNullOrWhiteSpace(telefone) ? null : SomenteDigitos(telefone),
            Endereco = string.IsNullOrWhiteSpace(endereco) ? null : endereco.Trim(),
            Status = EstabelecimentoStatus.Ativo,
            CriadoEm = DateTime.UtcNow,
            HorarioInicio = new TimeOnly(8, 0),
            HorarioFim = new TimeOnly(18, 0),
            DuracaoConsultaPadraoMinutos = 30,
            IntervaloEntreConsultasMinutos = 0,
            DiasSemanaFuncionamentoJson = "[1,2,3,4,5]",
            HorariosBloqueadosJson = "[]",
            DatasBloqueadasJson = "[]",
        };
    }

    /// <summary>
    /// Marca o aggregate como recém-criado (anexa <see cref="EstabelecimentoCriadoEvent"/>).
    /// Deve ser chamado pelo handler APÓS persistir o aggregate (para que o <see cref="Entity{TId}.Id"/>
    /// já esteja populado pelo EF Core).
    /// </summary>
    public virtual void MarcarComoCriado()
    {
        if (Id == 0)
            throw new InvalidOperationException("Estabelecimento ainda não foi persistido — Id é 0.");

        AddDomainEvent(new EstabelecimentoCriadoEvent(Id, DonoUsuarioId, NomeFantasia));
    }

    public virtual void AtualizarDados(
        string nomeFantasia,
        string razaoSocial,
        string cnpj,
        string telefone,
        string endereco)
    {
        if (string.IsNullOrWhiteSpace(nomeFantasia))
            throw new BusinessException("Nome fantasia é obrigatório.");

        var cnpjDigitos = string.IsNullOrWhiteSpace(cnpj) ? null : SomenteDigitos(cnpj);
        if (cnpjDigitos is { Length: > 0 and not 14 })
            throw new BusinessException("CNPJ deve conter 14 dígitos.");

        NomeFantasia = nomeFantasia.Trim();
        RazaoSocial = string.IsNullOrWhiteSpace(razaoSocial) ? null : razaoSocial.Trim();
        Cnpj = cnpjDigitos;
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : SomenteDigitos(telefone);
        Endereco = string.IsNullOrWhiteSpace(endereco) ? null : endereco.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Substitui toda a configuração de funcionamento (horários, dias, bloqueios).
    /// Itens em <paramref name="horariosBloqueados"/> ou <paramref name="datasBloqueadas"/>
    /// com <c>Id == Guid.Empty</c> recebem novo Id gerado pelo domínio.
    /// </summary>
    public virtual void AtualizarFuncionamento(
        TimeOnly horarioInicio,
        TimeOnly horarioFim,
        int duracaoConsultaPadraoMinutos,
        int intervaloEntreConsultasMinutos,
        IReadOnlyList<int> diasSemana,
        IReadOnlyList<HorarioBloqueado> horariosBloqueados,
        IReadOnlyList<DataBloqueada> datasBloqueadas)
    {
        if (horarioFim <= horarioInicio)
            throw new BusinessException("Horário de término deve ser maior que o horário de início.");
        if (duracaoConsultaPadraoMinutos < 5 || duracaoConsultaPadraoMinutos > 480)
            throw new BusinessException("Duração padrão da consulta deve estar entre 5 e 480 minutos.");
        if (intervaloEntreConsultasMinutos < 0 || intervaloEntreConsultasMinutos > 240)
            throw new BusinessException("Intervalo entre consultas deve estar entre 0 e 240 minutos.");
        if (diasSemana == null || diasSemana.Count == 0)
            throw new BusinessException("Selecione pelo menos um dia de funcionamento.");
        foreach (var d in diasSemana)
            if (d < 0 || d > 6)
                throw new BusinessException("Dia da semana inválido (use 0=Domingo até 6=Sábado).");

        horariosBloqueados ??= Array.Empty<HorarioBloqueado>();
        datasBloqueadas ??= Array.Empty<DataBloqueada>();

        var bloqueiosNormalizados = new List<HorarioBloqueado>(horariosBloqueados.Count);
        foreach (var hb in horariosBloqueados)
        {
            if (hb.Fim <= hb.Inicio)
                throw new BusinessException("Em horários bloqueados, o término deve ser maior que o início.");
            if (hb.Inicio < horarioInicio || hb.Fim > horarioFim)
                throw new BusinessException("Horário bloqueado deve estar dentro do horário de funcionamento.");
            var id = hb.Id == Guid.Empty ? Guid.NewGuid() : hb.Id;
            var desc = string.IsNullOrWhiteSpace(hb.Descricao) ? string.Empty : hb.Descricao.Trim();
            bloqueiosNormalizados.Add(hb with { Id = id, Descricao = desc });
        }

        var datasNormalizadas = new List<DataBloqueada>(datasBloqueadas.Count);
        var datasJaVistas = new HashSet<DateOnly>();
        foreach (var db in datasBloqueadas)
        {
            if (!datasJaVistas.Add(db.Data))
                throw new BusinessException($"Data bloqueada duplicada: {db.Data:yyyy-MM-dd}.");
            var id = db.Id == Guid.Empty ? Guid.NewGuid() : db.Id;
            var desc = string.IsNullOrWhiteSpace(db.Descricao) ? string.Empty : db.Descricao.Trim();
            datasNormalizadas.Add(db with { Id = id, Descricao = desc });
        }

        HorarioInicio = horarioInicio;
        HorarioFim = horarioFim;
        DuracaoConsultaPadraoMinutos = duracaoConsultaPadraoMinutos;
        IntervaloEntreConsultasMinutos = intervaloEntreConsultasMinutos;
        DiasSemanaFuncionamentoJson = JsonSerializer.Serialize(diasSemana.Distinct().OrderBy(d => d));
        HorariosBloqueadosJson = JsonSerializer.Serialize(bloqueiosNormalizados, _jsonOpts);
        DatasBloqueadasJson = JsonSerializer.Serialize(datasNormalizadas, _jsonOpts);
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Valida se um agendamento pode ocorrer no intervalo informado, respeitando a configuração
    /// de funcionamento (dia da semana, datas bloqueadas, faixa de horário, horários bloqueados),
    /// proibindo agendamentos no passado e exigindo que o fim caiba dentro do expediente do mesmo dia.
    /// Os parâmetros devem ser início, fim e "agora" em horário local.
    /// </summary>
    public virtual void ValidarPodeAgendar(DateTime inicioLocal, DateTime fimLocal, DateTime agoraLocal)
    {
        if (fimLocal <= inicioLocal)
            throw new BusinessException("O fim do agendamento deve ser posterior ao início.");

        if (inicioLocal < agoraLocal)
            throw new BusinessException("Não é possível agendar para uma data ou horário no passado.");

        var data = DateOnly.FromDateTime(inicioLocal);
        var horaInicio = TimeOnly.FromDateTime(inicioLocal);
        var horaFim = TimeOnly.FromDateTime(fimLocal);
        var diaSemana = (int)inicioLocal.DayOfWeek;

        if (!DiasSemanaFuncionamento.Contains(diaSemana))
            throw new BusinessException("O estabelecimento não funciona neste dia da semana.");

        if (DatasBloqueadas.Any(db => db.Data == data))
            throw new BusinessException("Esta data está bloqueada no estabelecimento.");

        if (horaInicio < HorarioInicio || horaInicio >= HorarioFim)
            throw new BusinessException(
                $"O agendamento deve estar dentro do horário de funcionamento " +
                $"({HorarioInicio:HH\\:mm}–{HorarioFim:HH\\:mm}).");

        // Não pode atravessar a meia-noite nem ultrapassar o fim do expediente.
        if (DateOnly.FromDateTime(fimLocal) != data || horaFim > HorarioFim)
            throw new BusinessException(
                $"O agendamento ultrapassa o horário de funcionamento " +
                $"({HorarioInicio:HH\\:mm}–{HorarioFim:HH\\:mm}). " +
                "Reduza a duração ou escolha um horário anterior.");

        var bloqueio = HorariosBloqueados.FirstOrDefault(hb =>
            horaInicio < hb.Fim && horaFim > hb.Inicio);
        if (bloqueio is not null)
        {
            var desc = string.IsNullOrWhiteSpace(bloqueio.Descricao) ? "" : $" ({bloqueio.Descricao})";
            throw new BusinessException(
                $"Este horário está bloqueado{desc}: {bloqueio.Inicio:HH\\:mm}–{bloqueio.Fim:HH\\:mm}.");
        }
    }

    public virtual void AlterarFoto(string fotoUrl)
    {
        if (string.IsNullOrWhiteSpace(fotoUrl))
            throw new BusinessException("URL da foto é obrigatória.");

        FotoUrl = fotoUrl.Trim();
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Limpa a foto/logo do estabelecimento. Idempotente — se já não havia foto,
    /// não atualiza <see cref="AtualizadoEm"/> (evita ruído em audit). O caller
    /// (handler) é quem apaga o blob no storage.
    /// </summary>
    public virtual void RemoverFoto()
    {
        if (string.IsNullOrWhiteSpace(FotoUrl)) return;

        FotoUrl = null;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Inativar()
    {
        if (Status == EstabelecimentoStatus.Inativo)
            throw new BusinessException("Estabelecimento já está inativo.");

        Status = EstabelecimentoStatus.Inativo;
        AtualizadoEm = DateTime.UtcNow;
    }

    public virtual void Reativar()
    {
        if (Status == EstabelecimentoStatus.Ativo)
            throw new BusinessException("Estabelecimento já está ativo.");

        Status = EstabelecimentoStatus.Ativo;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static string SomenteDigitos(string valor) =>
        new(valor.Where(char.IsDigit).ToArray());
}
