namespace Imedto.Backend.SharedKernel.Filters;

/// <summary>
/// Marca um endpoint como acessível mesmo quando o usuário ainda não completou o onboarding.
/// Usado para endpoints do próprio fluxo de onboarding (completar cadastro, verificar CPF, listar convites).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowBeforeOnboardingAttribute : Attribute { }
