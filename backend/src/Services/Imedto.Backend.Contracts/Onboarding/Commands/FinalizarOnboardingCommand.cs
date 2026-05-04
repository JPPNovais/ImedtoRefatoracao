using Imedto.Backend.SharedKernel.Cqrs;

namespace Imedto.Backend.Contracts.Onboarding.Commands;

public class FinalizarOnboardingCommand : ICommand
{
    // Perfil do usuário — sempre obrigatório.
    public string NomeCompleto { get; set; }
    public string Cpf { get; set; }
    public string Telefone { get; set; }

    // Preenchido apenas quando o usuário escolheu "sou dono".
    public EstabelecimentoOnboardingInput Estabelecimento { get; set; }

    // Preenchido quando o usuário informou conselho/especialidade.
    public ProfissionalOnboardingInput Profissional { get; set; }

    // Preenchido junto com Estabelecimento (horários da clínica).
    public FuncionamentoOnboardingInput Funcionamento { get; set; }
}

public class EstabelecimentoOnboardingInput
{
    public string NomeFantasia { get; set; }
    public string Cnpj { get; set; }
    public string Telefone { get; set; }
    public string Endereco { get; set; }
}

public class ProfissionalOnboardingInput
{
    public string Conselho { get; set; }
    public string Uf { get; set; }
    public string NumeroRegistro { get; set; }
    public string Especialidade { get; set; }
}

public class FuncionamentoOnboardingInput
{
    public string HorarioInicio { get; set; }
    public string HorarioFim { get; set; }
    public int DuracaoConsultaPadraoMinutos { get; set; } = 30;
    public int IntervaloEntreConsultasMinutos { get; set; } = 0;
    public IReadOnlyList<int> DiasSemana { get; set; } = Array.Empty<int>();
}
