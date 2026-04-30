using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

/// <summary>
/// Tabela de valor de honorários do profissional (catálogo). Define o cálculo de
/// honorários com base em tempo de cirurgia: <see cref="TempoBaseMinutos"/> +
/// <see cref="ValorTempoBase"/> como mínimo; cada bloco adicional de
/// <see cref="TempoAdicionalMinutos"/> minutos soma <see cref="ValorAdicional"/>;
/// <see cref="ValorPlus"/> é fixo (ex: pernoite, sobressalente).
///
/// O <c>ProfissionalUsuarioId</c> é opcional — quando null, é uma "tabela
/// padrão" da função (ex: tabela genérica de auxiliar). Quando preenchido, é
/// específica daquele profissional naquela função no estabelecimento.
/// </summary>
public class ValorProfissionalOrcamento : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual Guid? ProfissionalUsuarioId { get; protected set; }
    public virtual string Funcao { get; protected set; } = string.Empty;
    public virtual int TempoBaseMinutos { get; protected set; }
    public virtual decimal ValorTempoBase { get; protected set; }
    public virtual int TempoAdicionalMinutos { get; protected set; }
    public virtual decimal ValorAdicional { get; protected set; }
    public virtual decimal ValorPlus { get; protected set; }
    public virtual bool Ativo { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected ValorProfissionalOrcamento() { }

    public static ValorProfissionalOrcamento Criar(
        long estabelecimentoId,
        Guid? profissionalUsuarioId,
        string funcao,
        int tempoBaseMinutos,
        decimal valorTempoBase,
        int tempoAdicionalMinutos,
        decimal valorAdicional,
        decimal valorPlus)
    {
        Validar(estabelecimentoId, funcao, tempoBaseMinutos, valorTempoBase,
                tempoAdicionalMinutos, valorAdicional, valorPlus);
        return new ValorProfissionalOrcamento
        {
            EstabelecimentoId = estabelecimentoId,
            ProfissionalUsuarioId = profissionalUsuarioId,
            Funcao = funcao.Trim(),
            TempoBaseMinutos = tempoBaseMinutos,
            ValorTempoBase = valorTempoBase,
            TempoAdicionalMinutos = tempoAdicionalMinutos,
            ValorAdicional = valorAdicional,
            ValorPlus = valorPlus,
            Ativo = true,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(
        string funcao,
        int tempoBaseMinutos,
        decimal valorTempoBase,
        int tempoAdicionalMinutos,
        decimal valorAdicional,
        decimal valorPlus)
    {
        Validar(EstabelecimentoId, funcao, tempoBaseMinutos, valorTempoBase,
                tempoAdicionalMinutos, valorAdicional, valorPlus);
        Funcao = funcao.Trim();
        TempoBaseMinutos = tempoBaseMinutos;
        ValorTempoBase = valorTempoBase;
        TempoAdicionalMinutos = tempoAdicionalMinutos;
        ValorAdicional = valorAdicional;
        ValorPlus = valorPlus;
        AtualizadaEm = DateTime.UtcNow;
    }

    public virtual void Inativar() { if (Ativo) { Ativo = false; AtualizadaEm = DateTime.UtcNow; } }
    public virtual void Reativar() { if (!Ativo) { Ativo = true; AtualizadaEm = DateTime.UtcNow; } }

    private static void Validar(long estab, string funcao, int tempoBase, decimal valorBase,
        int tempoAdc, decimal valorAdc, decimal valorPlus)
    {
        if (estab <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (string.IsNullOrWhiteSpace(funcao)) throw new BusinessException("Função é obrigatória.");
        if (tempoBase <= 0) throw new BusinessException("Tempo base deve ser positivo.");
        if (valorBase < 0) throw new BusinessException("Valor do tempo base não pode ser negativo.");
        if (tempoAdc <= 0) throw new BusinessException("Tempo adicional deve ser positivo.");
        if (valorAdc < 0) throw new BusinessException("Valor adicional não pode ser negativo.");
        if (valorPlus < 0) throw new BusinessException("Valor plus não pode ser negativo.");
    }
}
