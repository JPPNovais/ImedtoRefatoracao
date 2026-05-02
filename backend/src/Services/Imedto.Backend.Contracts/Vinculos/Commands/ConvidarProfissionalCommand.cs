using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

/// <summary>
/// Cria o vínculo local (status Convidado) após o backend já ter criado/identificado
/// o usuário do profissional no Supabase Auth. O controller orquestra a chamada ao
/// Supabase e dispara este command com o <see cref="ProfissionalUsuarioId"/> resolvido.
/// </summary>
public class ConvidarProfissionalCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public Guid ConvidadoPorUsuarioId { get; set; }
    public Guid ProfissionalUsuarioId { get; set; }
    public string ProfissionalEmail { get; set; }
    public long? ModeloPermissaoId { get; set; }

    /// <summary>Dados pré-cadastrados pelo convidador para o onboarding (todos opcionais).</summary>
    public string Nome { get; set; }
    public string Telefone { get; set; }
    public string Especialidade { get; set; }
}
