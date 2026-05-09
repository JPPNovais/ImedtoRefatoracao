namespace Imedto.Backend.Domain.Auth;

/// <summary>
/// Resultado de <see cref="IAuthService.CriarConviteAsync"/>.
/// </summary>
/// <param name="User">Usuário (novo ou existente) identificado pelo email.</param>
/// <param name="ActionLink">
/// Magic link de invite — o usuário clica, define senha e entra logado.
/// É <c>null</c> quando <paramref name="JaExistia"/> é <c>true</c>.
/// </param>
/// <param name="JaExistia">Indica se o usuário já tinha conta de auth antes deste convite.</param>
public record ConviteResult(UserInfo User, string ActionLink, bool JaExistia);
