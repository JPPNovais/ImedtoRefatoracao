using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Estabelecimentos.Commands;

/// <summary>
/// Atualiza o toggle "Exigir 2FA para o papel Dono" do estabelecimento.
/// Só pode ser executado pelo Dono do estabelecimento (R9/CA13).
/// </summary>
public class AtualizarExigirDono2faCommand : ICommand
{
    public long EstabelecimentoId { get; set; }
    public bool Exigir { get; set; }
}
