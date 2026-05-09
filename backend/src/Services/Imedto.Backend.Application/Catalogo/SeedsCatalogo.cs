namespace Imedto.Backend.Application.Catalogo;

/// <summary>
/// Dados de seed do catálogo de profissões e especialidades brasileiras.
///
/// Esta estrutura estática é consumida pelo database-architect na Wave 5
/// para gerar os INSERTs SQL em db/migrations/.
///
/// Regra: a migration de seed é responsabilidade do database-architect (Wave 5).
/// </summary>
public static class SeedsCatalogo
{
    public record ProfissaoSeed(string Nome, string ConselhoSigla, IReadOnlyList<string> Especialidades);

    public static readonly IReadOnlyList<ProfissaoSeed> Profissoes = new List<ProfissaoSeed>
    {
        new("Médico", "CRM",
        [
            "Clínico Geral",
            "Cardiologia",
            "Pediatria",
            "Ginecologia e Obstetrícia",
            "Ortopedia e Traumatologia",
            "Dermatologia",
            "Endocrinologia",
            "Psiquiatria",
            "Gastroenterologia",
            "Oftalmologia",
            "Geriatria",
            "Neurologia",
            "Anestesiologia",
            "Cirurgia Geral",
            "Cirurgia Plástica",
            "Infectologia",
            "Nefrologia",
            "Pneumologia",
            "Reumatologia",
            "Urologia",
            "Hematologia",
            "Oncologia",
            "Radiologia",
            "Medicina do Trabalho",
            "Medicina Esportiva",
            "Medicina de Família e Comunidade",
            "Alergia e Imunologia",
            "Medicina Intensiva",
            "Otorrinolaringologia",
            "Nutrologia",
            "Neonatologia",
            "Neurocirurgia",
            "Medicina Paliativa",
            "Patologia",
            "Mastologia"
        ]),

        new("Dentista", "CRO",
        [
            "Odontologia Geral",
            "Odontopediatria",
            "Ortodontia",
            "Periodontia",
            "Endodontia",
            "Implantodontia",
            "Prótese Dentária",
            "Cirurgia e Traumatologia Bucomaxilofacial",
            "Dentística",
            "Odontologia Estética",
            "Odontogeriatria",
            "Patologia Oral",
            "Radiologia Odontológica",
            "Estomatologia",
            "Harmonização Orofacial",
            "Odontologia do Trabalho",
            "Odontologia Legal",
            "Saúde Coletiva (Odontologia)"
        ]),

        new("Fisioterapeuta", "CREFITO",
        [
            "Fisioterapia Geral",
            "Fisioterapia Traumato-Ortopédica",
            "Fisioterapia Neurofuncional",
            "Fisioterapia Cardiorrespiratória",
            "Fisioterapia Dermatofuncional",
            "Fisioterapia Uroginecológica",
            "Fisioterapia em Terapia Intensiva",
            "Fisioterapia Esportiva",
            "Fisioterapia do Trabalho",
            "Fisioterapia Aquática",
            "Fisioterapia Oncológica",
            "Fisioterapia em Gerontologia",
            "Fisioterapia Pediátrica"
        ]),

        new("Psicólogo", "CRP",
        [
            "Psicologia Clínica",
            "Psicologia Organizacional e do Trabalho",
            "Psicologia Escolar e Educacional",
            "Psicologia Jurídica",
            "Psicologia Social",
            "Psicologia Hospitalar",
            "Neuropsicologia",
            "Psicologia do Esporte",
            "Terapia Cognitivo-Comportamental",
            "Psicanálise",
            "Terapia Familiar",
            "Psicologia Infantil"
        ]),

        new("Nutricionista", "CRN",
        [
            "Nutrição Clínica",
            "Nutrição Esportiva",
            "Nutrição Materno-Infantil",
            "Nutrição Oncológica",
            "Nutrição em Cardiologia",
            "Nutrição em Nefrologia",
            "Nutrição Comportamental",
            "Nutrição Funcional",
            "Fitoterapia Aplicada à Nutrição"
        ]),

        new("Fonoaudiólogo", "CRFa",
        [
            "Fonoaudiologia Geral",
            "Audiologia",
            "Linguagem",
            "Motricidade Orofacial",
            "Voz",
            "Disfagia",
            "Fonoaudiologia Educacional",
            "Fonoaudiologia Neurofuncional",
            "Fonoaudiologia Hospitalar"
        ]),

        new("Enfermeiro", "COREN",
        [
            "Enfermagem Geral",
            "Enfermagem Obstétrica",
            "Enfermagem Pediátrica",
            "Enfermagem em Centro Cirúrgico",
            "Enfermagem em Terapia Intensiva",
            "Enfermagem do Trabalho",
            "Enfermagem em Cardiologia",
            "Enfermagem em Oncologia",
            "Enfermagem em Nefrologia",
            "Enfermagem em Saúde Mental",
            "Enfermagem em Emergência",
            "Enfermagem Domiciliar",
            "Enfermagem Estética"
        ]),

        new("Terapeuta Ocupacional", "CREFITO",
        [
            "Terapia Ocupacional Geral",
            "Terapia Ocupacional Pediátrica",
            "Terapia Ocupacional em Saúde Mental",
            "Terapia Ocupacional Neurológica",
            "Terapia Ocupacional em Gerontologia",
            "Terapia Ocupacional em Reabilitação Física",
            "Terapia Ocupacional Social"
        ]),

        new("Farmacêutico", "CRF",
        [
            "Farmácia Clínica",
            "Farmácia Hospitalar",
            "Farmácia Industrial",
            "Farmácia de Manipulação",
            "Farmacologia Clínica",
            "Toxicologia",
            "Análises Clínicas"
        ]),

        new("Biomédico", "CFBM",
        [
            "Biomedicina Clínica",
            "Hematologia",
            "Microbiologia",
            "Parasitologia",
            "Biologia Molecular",
            "Citologia",
            "Imunologia"
        ]),

        new("Veterinário", "CFMV",
        [
            "Clínica de Pequenos Animais",
            "Clínica de Grandes Animais",
            "Cirurgia Veterinária",
            "Dermatologia Veterinária",
            "Oftalmologia Veterinária",
            "Oncologia Veterinária",
            "Cardiologia Veterinária",
            "Medicina Preventiva Veterinária"
        ]),

        new("Técnico de Enfermagem", "",
        [
            "Técnico em Enfermagem Geral",
            "Técnico em Saúde Bucal"
        ]),

        new("Educador Físico", "CONFEF",
        [
            "Personal Trainer",
            "Musculação",
            "Treinamento Funcional",
            "Pilates",
            "Natação",
            "Corrida e Atletismo"
        ]),

        new("Assistente Social", "CRESS",
        [
            "Assistência Social em Saúde",
            "Assistência Social Hospitalar",
            "Assistência Social Oncológica"
        ]),

        new("Médico Veterinário Residente", "",
        [
            "Residência em Pequenos Animais",
            "Residência em Grandes Animais"
        ]),

        new("Acupunturista", "",
        [
            "Acupuntura Tradicional Chinesa",
            "Auriculoterapia",
            "Moxibustão"
        ]),

        new("Quiropraxista", "",
        [
            "Quiropraxia Geral",
            "Quiropraxia Esportiva",
            "Quiropraxia Pediátrica"
        ]),

        new("Médico Radiologista", "CRM",
        [
            "Radiologia Geral",
            "Tomografia Computadorizada",
            "Ressonância Magnética",
            "Ultrassonografia",
            "Mamografia"
        ]),

        new("Cirurgião-Dentista Especialista", "CRO",
        [
            "Disfunção Temporomandibular",
            "Odontologia do Sono"
        ]),

        new("Podólogo", "",
        [
            "Podologia Geral",
            "Podologia Esportiva",
            "Podologia em Diabetologia"
        ]),

        new("Optometrista", "",
        [
            "Optometria Clínica",
            "Visão Subnormal",
            "Lentes de Contato"
        ]),

        new("Psicopedagogo", "",
        [
            "Psicopedagogia Clínica",
            "Psicopedagogia Institucional"
        ]),

        new("Gerontólogo", "",
        [
            "Gerontologia Clínica",
            "Gerontologia Social"
        ]),

        new("Homeopata", "",
        [
            "Homeopatia Clínica",
            "Homeopatia Veterinária"
        ]),

        new("Osteopata", "",
        [
            "Osteopatia Geral",
            "Osteopatia Visceral",
            "Osteopatia Craniosacral"
        ]),

        new("Massoterapeuta", "",
        [
            "Massoterapia Relaxante",
            "Massoterapia Terapêutica",
            "Drenagem Linfática"
        ]),

        new("Pilates", "",
        [
            "Pilates Solo",
            "Pilates com Aparelhos",
            "Pilates para Gestantes",
            "Pilates Clínico"
        ]),

        new("Neuropsicopedagogo", "",
        [
            "Neuropsicopedagogia Clínica",
            "Neuropsicopedagogia Institucional"
        ]),

        new("Médico do Esporte", "CRM",
        [
            "Medicina Esportiva Clínica",
            "Fisiologia do Exercício",
            "Prevenção de Lesões"
        ]),

        new("Médico Nutrólogo", "CRM",
        [
            "Nutrologia Clínica",
            "Nutrologia do Esporte",
            "Nutrologia Pediátrica"
        ]),

        new("Terapeuta ABA", "",
        [
            "Análise do Comportamento Aplicada",
            "Terapia ABA para TEA"
        ])
    };
}
