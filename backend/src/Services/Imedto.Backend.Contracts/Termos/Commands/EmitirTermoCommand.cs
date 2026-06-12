using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Termos.Commands;

/// <summary>
/// Emite um termo de consentimento (documento físico) para um paciente.
/// O handler resolve variáveis, sanitiza o resultado, calcula hash de integridade
/// e cria o aggregate TermoEmitido com status Pendente (aguardando anexo do documento).
///
/// O aceite por link público foi removido (briefing 2026-06-12_002). O único fluxo
/// de assinatura é o documento físico (foto JPG/PNG ou PDF, enviado pelo endpoint de anexo).
/// </summary>
public class EmitirTermoCommand : ICommand
{
    public long PacienteId { get; set; }
    public long EstabelecimentoId { get; set; }
    public Guid EmissorUsuarioId { get; set; }

    /// <summary>
    /// Quando informado, deve corresponder a um profissional com vínculo Ativo no estabelecimento.
    /// É esse usuário que aparece nas variáveis <c>{{profissional.*}}</c> do snapshot.
    /// Quando ausente (null/Guid.Empty), as variáveis caem para o fallback <c>___________</c>.
    /// </summary>
    public Guid? ProfissionalUsuarioId { get; set; }

    public long ModeloId { get; set; }

    /// <summary>
    /// Quando emitido dentro de uma evolução de prontuário, vincula o termo à evolução
    /// para exibição na timeline. Nulo para emissões avulsas (aba de termos do paciente).
    /// </summary>
    public long? EvolucaoId { get; set; }

    /// <summary>Preenchido pelo handler — id do termo emitido.</summary>
    public long TermoEmitidoId { get; set; }
}
