using Imedto.Backend.SharedKernel.Domain;

namespace Imedto.Backend.Domain.Orcamentos.Catalogos;

/// <summary>
/// Configuração de valores de "local de cirurgia" por tipo de internação. Cada
/// estabelecimento tem 1 linha por <see cref="TipoInternacao"/>. Usado no cálculo
/// de orçamento: <see cref="TempoBaseMinutos"/> base + períodos adicionais de
/// <see cref="TempoAdicionalMinutos"/> minutos com valor <see cref="ValorAdicional"/>
/// cada.
/// </summary>
public class ConfiguracaoLocalCirurgia : Entity
{
    public virtual long EstabelecimentoId { get; protected set; }
    public virtual TipoInternacao TipoInternacao { get; protected set; }
    public virtual int TempoBaseMinutos { get; protected set; }
    public virtual decimal ValorBase { get; protected set; }
    public virtual int TempoAdicionalMinutos { get; protected set; }
    public virtual decimal ValorAdicional { get; protected set; }
    public virtual DateTime CriadaEm { get; protected set; }
    public virtual DateTime? AtualizadaEm { get; protected set; }

    protected ConfiguracaoLocalCirurgia() { }

    public static ConfiguracaoLocalCirurgia Criar(
        long estabelecimentoId,
        TipoInternacao tipo,
        int tempoBaseMinutos,
        decimal valorBase,
        int tempoAdicionalMinutos,
        decimal valorAdicional)
    {
        Validar(estabelecimentoId, tempoBaseMinutos, valorBase, tempoAdicionalMinutos, valorAdicional);
        return new ConfiguracaoLocalCirurgia
        {
            EstabelecimentoId = estabelecimentoId,
            TipoInternacao = tipo,
            TempoBaseMinutos = tempoBaseMinutos,
            ValorBase = valorBase,
            TempoAdicionalMinutos = tempoAdicionalMinutos,
            ValorAdicional = valorAdicional,
            CriadaEm = DateTime.UtcNow
        };
    }

    public virtual void Atualizar(
        int tempoBaseMinutos,
        decimal valorBase,
        int tempoAdicionalMinutos,
        decimal valorAdicional)
    {
        Validar(EstabelecimentoId, tempoBaseMinutos, valorBase, tempoAdicionalMinutos, valorAdicional);
        TempoBaseMinutos = tempoBaseMinutos;
        ValorBase = valorBase;
        TempoAdicionalMinutos = tempoAdicionalMinutos;
        ValorAdicional = valorAdicional;
        AtualizadaEm = DateTime.UtcNow;
    }

    private static void Validar(long estab, int tempoBase, decimal valorBase, int tempoAdc, decimal valorAdc)
    {
        if (estab <= 0) throw new BusinessException("Estabelecimento é obrigatório.");
        if (tempoBase <= 0) throw new BusinessException("Tempo base deve ser positivo.");
        if (valorBase < 0) throw new BusinessException("Valor base não pode ser negativo.");
        if (tempoAdc <= 0) throw new BusinessException("Tempo adicional deve ser positivo.");
        if (valorAdc < 0) throw new BusinessException("Valor adicional não pode ser negativo.");
    }
}
