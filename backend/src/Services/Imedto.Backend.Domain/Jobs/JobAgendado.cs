using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Jobs;

/// <summary>
/// Aggregate root de um job persistido na tabela <c>jobs_agendados</c>.
/// O scheduler trata cada linha como um pequeno workflow:
/// Pendente → Executando → (Pendente reagendado | Concluido | Falhou).
/// One-shot (<see cref="IntervaloSeg"/> = 0) finaliza em <see cref="JobStatus.Concluido"/>.
/// Recorrente volta para <see cref="JobStatus.Pendente"/> com <see cref="ProximoRunEm"/> avançado.
/// </summary>
public class JobAgendado : Entity
{
    /// <summary>Limite de tentativas antes de marcar o job como <see cref="JobStatus.Falhou"/>.</summary>
    public const int MaxTentativas = 5;

    public virtual string Nome { get; protected set; } = string.Empty;
    public virtual DateTime ProximoRunEm { get; protected set; }
    public virtual DateTime? UltimoRunEm { get; protected set; }
    public virtual int IntervaloSeg { get; protected set; }
    public virtual JobStatus Status { get; protected set; }
    public virtual string UltimaFalha { get; protected set; }
    public virtual int Tentativas { get; protected set; }
    public virtual DateTime CriadoEm { get; protected set; }
    public virtual DateTime? AtualizadoEm { get; protected set; }

    protected JobAgendado() { }

    /// <summary>
    /// Cria um novo job. <paramref name="intervaloSeg"/> = 0 indica one-shot;
    /// qualquer valor positivo é o intervalo entre execuções consecutivas.
    /// </summary>
    public static JobAgendado Agendar(string nome, DateTime primeiroRunEm, int intervaloSeg)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new BusinessException("Nome do job é obrigatório.");
        if (intervaloSeg < 0)
            throw new BusinessException("Intervalo do job não pode ser negativo.");

        return new JobAgendado
        {
            Nome = nome.Trim(),
            ProximoRunEm = primeiroRunEm,
            IntervaloSeg = intervaloSeg,
            Status = JobStatus.Pendente,
            Tentativas = 0,
            CriadoEm = DateTime.UtcNow
        };
    }

    /// <summary>Marca o job como em execução. Só pode partir de Pendente.</summary>
    public virtual void MarcarExecutando()
    {
        if (Status != JobStatus.Pendente)
            throw new BusinessException($"Job '{Nome}' não está pendente (status atual: {Status}).");

        Status = JobStatus.Executando;
        UltimoRunEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca o job como concluído. Recorrente volta para <see cref="JobStatus.Pendente"/> com
    /// <see cref="ProximoRunEm"/> avançado pelo intervalo. One-shot fica em <see cref="JobStatus.Concluido"/>.
    /// Tentativas e última falha são limpos no sucesso.
    /// </summary>
    public virtual void MarcarConcluido()
    {
        if (Status != JobStatus.Executando)
            throw new BusinessException($"Job '{Nome}' não está executando (status atual: {Status}).");

        Tentativas = 0;
        UltimaFalha = null;
        AtualizadoEm = DateTime.UtcNow;

        if (IntervaloSeg <= 0)
        {
            Status = JobStatus.Concluido;
            return;
        }

        Status = JobStatus.Pendente;
        ProximoRunEm = DateTime.UtcNow.AddSeconds(IntervaloSeg);
    }

    /// <summary>
    /// Registra falha de execução com backoff exponencial: próxima tentativa em
    /// <c>min(60s · 2^tentativas, 1h)</c>. Após <see cref="MaxTentativas"/> excedido,
    /// o job vai para <see cref="JobStatus.Falhou"/> e não é mais agendado automaticamente.
    /// </summary>
    public virtual void MarcarFalhou(string mensagemErro)
    {
        if (Status != JobStatus.Executando)
            throw new BusinessException($"Job '{Nome}' não está executando (status atual: {Status}).");

        Tentativas += 1;
        UltimaFalha = Truncar(mensagemErro, 500);
        AtualizadoEm = DateTime.UtcNow;

        if (Tentativas > MaxTentativas)
        {
            Status = JobStatus.Falhou;
            return;
        }

        Status = JobStatus.Pendente;
        ProximoRunEm = DateTime.UtcNow.Add(CalcularBackoff(Tentativas));
    }

    /// <summary>
    /// Reagendamento manual (bootstrap, intervenção via endpoint admin). Só vale quando o job
    /// está em estado terminal/parado — não preempta execução em curso.
    /// </summary>
    public virtual void Reagendar(DateTime proximoRunEm, int? novoIntervaloSeg = null)
    {
        if (Status == JobStatus.Executando)
            throw new BusinessException($"Job '{Nome}' está executando — não pode ser reagendado agora.");

        if (novoIntervaloSeg.HasValue)
        {
            if (novoIntervaloSeg.Value < 0)
                throw new BusinessException("Intervalo do job não pode ser negativo.");
            IntervaloSeg = novoIntervaloSeg.Value;
        }

        Status = JobStatus.Pendente;
        ProximoRunEm = proximoRunEm;
        Tentativas = 0;
        UltimaFalha = null;
        AtualizadoEm = DateTime.UtcNow;
    }

    private static TimeSpan CalcularBackoff(int tentativas)
    {
        // 60s, 120s, 240s, 480s, 960s, 1920s → cap em 1h.
        var segundos = 60.0 * Math.Pow(2, tentativas - 1);
        var capado = Math.Min(segundos, 3600.0);
        return TimeSpan.FromSeconds(capado);
    }

    private static string Truncar(string valor, int tamanho)
    {
        if (string.IsNullOrEmpty(valor)) return valor;
        return valor.Length <= tamanho ? valor : valor.Substring(0, tamanho);
    }
}
