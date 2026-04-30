namespace Imedto.Backend.Domain.Admin;

/// <summary>
/// Seleciona quais módulos de dados serão apagados no reset administrativo
/// de um estabelecimento. Cada flag é opt-in — false = não toca naquele módulo.
/// </summary>
public class ResetModulos
{
    /// <summary>Agendamentos.</summary>
    public bool Agenda { get; init; }

    /// <summary>Pacientes (e filhos via FK CASCADE: prontuários, evoluções, anexos, logs, exame físico, receitas, cirurgias).</summary>
    public bool Pacientes { get; init; }

    /// <summary>Prontuário (separado de Pacientes — limpa prontuário sem apagar o cadastro do paciente).</summary>
    public bool Prontuario { get; init; }

    /// <summary>Receitas médicas, itens de receita, medicamentos favoritos e configuração de receituário.</summary>
    public bool Receitas { get; init; }

    /// <summary>Registros de exame físico e regiões.</summary>
    public bool ExameFisico { get; init; }

    /// <summary>Procedimentos cirúrgicos e equipe cirúrgica.</summary>
    public bool Cirurgias { get; init; }

    /// <summary>Orçamentos e todas as tabelas filhas (itens, anestesia, implantes, internação, formas de pagamento, equipe).</summary>
    public bool Orcamentos { get; init; }

    /// <summary>Lançamentos financeiros, categorias e formas de pagamento.</summary>
    public bool Financeiro { get; init; }

    /// <summary>Movimentações de estoque e itens de inventário.</summary>
    public bool Inventario { get; init; }

    /// <summary>Regras e eventos de automação, configurações de automação.</summary>
    public bool Automacoes { get; init; }

    /// <summary>Notificações.</summary>
    public bool Notificacoes { get; init; }

    /// <summary>Solicitações de vínculo e vínculos profissional-estabelecimento.</summary>
    public bool Vinculos { get; init; }

    /// <summary>
    /// Configurações do estabelecimento: modelos de permissão, configurações de IA,
    /// assinaturas, variáveis de prontuário, modelos de prontuário, salas e unidades.
    /// Após o DELETE, recria os modelos de permissão padrão e seeds financeiros.
    /// </summary>
    public bool Configuracoes { get; init; }

    /// <summary>Retorna uma instância com todos os módulos habilitados (reset total).</summary>
    public static ResetModulos Tudo() => new()
    {
        Agenda = true,
        Pacientes = true,
        Prontuario = true,
        Receitas = true,
        ExameFisico = true,
        Cirurgias = true,
        Orcamentos = true,
        Financeiro = true,
        Inventario = true,
        Automacoes = true,
        Notificacoes = true,
        Vinculos = true,
        Configuracoes = true
    };
}
