namespace Imedto.Backend.Domain.Agendamentos;

/// <summary>Urgência da entrada na lista de espera (afeta priorização do encaixe).</summary>
public enum ListaEsperaPrioridade
{
    Rotina,
    Prioritario,
    Urgente,
}

/// <summary>Preferência de período do paciente para o encaixe.</summary>
public enum ListaEsperaPreferenciaPeriodo
{
    Qualquer,
    Manha,
    Tarde,
}
