using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Vinculos.Commands;

/// <summary>
/// Aceita TODOS os vínculos em status <c>Convidado</c> do usuário informado.
/// Comando idempotente — se não houver pendentes, é no-op silencioso.
///
/// Disparado pelo endpoint <c>POST /api/auth/aceitar-convite</c> imediatamente
/// após o convidado definir senha: ele veio do link do e-mail justamente para
/// se juntar a um estabelecimento, então faz parte do mesmo passo aceitar o
/// vínculo automaticamente — antes desta correção, o profissional entrava
/// logado mas o vínculo continuava "Convidado", forçando-o a abrir
/// <c>/meus-convites</c> e clicar "Aceitar" manualmente para entrar no tenant.
/// </summary>
public class AceitarConvitesPendentesDoUsuarioCommand : ICommand
{
    public Guid ProfissionalUsuarioId { get; set; }
}
