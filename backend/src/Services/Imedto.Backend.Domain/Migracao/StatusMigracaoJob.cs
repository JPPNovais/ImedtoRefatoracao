namespace Imedto.Backend.Domain.Migracao;

/// <summary>
/// Estados do job de migração. Cada transição é auditada (CA20).
/// </summary>
public enum StatusMigracaoJob
{
    /// <summary>Job criado, aguardando upload do arquivo (estado inicial).</summary>
    AguardandoArquivo,

    /// <summary>Arquivo recebido — pipeline descompactando e disparando inferência de mapa.</summary>
    AguardandoMapa,

    /// <summary>Mapa proposto pela IA — aguardando revisão do operador admin.</summary>
    MapaEmRevisao,

    /// <summary>Operador aprovou o mapa — preview gerado, aguardando disparo da carga.</summary>
    PreviewPronto,

    /// <summary>Carga em andamento (assíncrona via JobScheduler).</summary>
    Migrando,

    /// <summary>Carga concluída sem rejeições.</summary>
    Concluido,

    /// <summary>Carga concluída mas com registros rejeitados (relatório disponível).</summary>
    ConcluidoComErros,

    /// <summary>Job desfeito (apenas criados revertidos — R9).</summary>
    Desfeito,

    /// <summary>Rejeitado antes de iniciar (ex.: arquivo > 50MB).</summary>
    Rejeitado
}
