using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

/// <summary>
/// Cria o vínculo local (status Convidado) após o backend já ter criado/identificado
/// a credencial de auth do profissional. O controller orquestra a criação do convite
/// e dispara este command com o <see cref="ProfissionalUsuarioId"/> resolvido.
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

    /// <summary>
    /// Profissão do catálogo (obrigatória quando <see cref="Especialidade"/> for informada).
    /// Validada contra <c>profissoes</c>; especialidade tem que pertencer a esta profissão.
    /// </summary>
    public long? ProfissaoId { get; set; }
}
