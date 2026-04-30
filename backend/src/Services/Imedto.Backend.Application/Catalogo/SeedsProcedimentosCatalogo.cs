namespace Imedto.Backend.Application.Catalogo;

/// <summary>
/// Seed estático do catálogo TUSS/CBHPM de procedimentos comuns.
///
/// Esta estrutura é consumida pelo database-architect na Wave de migrations
/// para gerar os INSERTs SQL idempotentes em supabase/migrations/.
///
/// Fonte dos códigos: Terminologia Unificada da Saúde Suplementar (ANS/TUSS)
/// e Classificação Brasileira Hierarquizada de Procedimentos Médicos (AMB/CBHPM).
/// </summary>
public static class SeedsProcedimentosCatalogo
{
    public record ProcedimentoSeed(string Codigo, string Nome, string Origem, string? Capitulo);

    public static readonly IReadOnlyList<ProcedimentoSeed> Procedimentos = new List<ProcedimentoSeed>
    {
        // ── Consultas ──────────────────────────────────────────────────────────────
        new("10101012", "Consulta médica em atenção primária", "TUSS", "Consultas"),
        new("10101039", "Consulta médica em consultório (horário normal)", "TUSS", "Consultas"),
        new("10101047", "Consulta médica em pronto atendimento/urgência", "TUSS", "Consultas"),
        new("10102016", "Consulta odontológica", "TUSS", "Consultas"),
        new("10103011", "Consulta de enfermagem", "TUSS", "Consultas"),
        new("10105014", "Consulta nutricional", "TUSS", "Consultas"),
        new("10104010", "Consulta em psicologia", "TUSS", "Consultas"),

        // ── Cirurgias Plásticas ────────────────────────────────────────────────────
        new("30724019", "Rinoplastia", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724027", "Mentoplastia", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724035", "Otoplastia", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724043", "Blefaroplastia superior", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724051", "Blefaroplastia inferior", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724078", "Ritidoplastia (face lift)", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724094", "Mamoplastia redutora", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724105", "Lipoaspiração", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724113", "Abdominoplastia", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724121", "Lipoenxertia (lipofilling)", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724148", "Mamoplastia de aumento (implante mamário)", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724156", "Mastopexia", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724164", "Braquioplastia", "TUSS", "Cirurgia / Sistema tegumentar"),
        new("30724172", "Gluteoplastia", "TUSS", "Cirurgia / Sistema tegumentar"),

        // ── Cirurgias digestivas ───────────────────────────────────────────────────
        new("30602025", "Apendicectomia", "TUSS", "Cirurgia / Sistema digestivo"),
        new("30602033", "Colecistectomia laparoscópica", "TUSS", "Cirurgia / Sistema digestivo"),
        new("30602041", "Herniorrafia inguinal (unilateral)", "TUSS", "Cirurgia / Sistema digestivo"),
        new("30602050", "Herniorrafia umbilical", "TUSS", "Cirurgia / Sistema digestivo"),
        new("30602068", "Gastrectomia parcial", "TUSS", "Cirurgia / Sistema digestivo"),
        new("30602076", "Colectomia (ressecção do cólon)", "TUSS", "Cirurgia / Sistema digestivo"),

        // ── Cirurgias ortopédicas ──────────────────────────────────────────────────
        new("30901014", "Artroscopia de joelho diagnóstica", "TUSS", "Cirurgia / Sistema musculoesquelético"),
        new("30901022", "Artroscopia de joelho com meniscectomia", "TUSS", "Cirurgia / Sistema musculoesquelético"),
        new("30901030", "Reconstrução do ligamento cruzado anterior", "TUSS", "Cirurgia / Sistema musculoesquelético"),
        new("30901049", "Artroscopia de ombro", "TUSS", "Cirurgia / Sistema musculoesquelético"),
        new("30910014", "Osteossíntese de fratura de fêmur", "TUSS", "Cirurgia / Sistema musculoesquelético"),
        new("30910022", "Artroplastia total de quadril", "TUSS", "Cirurgia / Sistema musculoesquelético"),
        new("30910030", "Artroplastia total de joelho", "TUSS", "Cirurgia / Sistema musculoesquelético"),

        // ── Endoscopias ────────────────────────────────────────────────────────────
        new("40307054", "Endoscopia digestiva alta", "TUSS", "Endoscopia"),
        new("40307062", "Colonoscopia", "TUSS", "Endoscopia"),
        new("40307070", "Colonoscopia com polipectomia", "TUSS", "Endoscopia"),
        new("40307089", "Videolaparoscopia diagnóstica", "TUSS", "Endoscopia"),
        new("40308040", "Broncoscopia diagnóstica", "TUSS", "Endoscopia"),

        // ── Exames laboratoriais e de imagem (TUSS) ────────────────────────────────
        new("40312027", "Eletrocardiograma (ECG)", "TUSS", "Cardiologia / Diagnóstico"),
        new("40312035", "Ecocardiograma transtorácico", "TUSS", "Cardiologia / Diagnóstico"),
        new("40312043", "Holter 24h", "TUSS", "Cardiologia / Diagnóstico"),
        new("40312051", "Teste ergométrico (ergometria)", "TUSS", "Cardiologia / Diagnóstico"),
        new("40801026", "Tomografia computadorizada de crânio", "TUSS", "Radiologia"),
        new("40801034", "Tomografia computadorizada de tórax", "TUSS", "Radiologia"),
        new("40801042", "Ressonância magnética de coluna lombar", "TUSS", "Radiologia"),
        new("40801050", "Ressonância magnética de joelho", "TUSS", "Radiologia"),
        new("40901017", "Ultrassonografia de abdome total", "TUSS", "Radiologia"),
        new("40901025", "Ultrassonografia pélvica transvaginal", "TUSS", "Radiologia"),
        new("40901033", "Ultrassonografia de mama bilateral", "TUSS", "Radiologia"),
        new("40901041", "Mamografia bilateral", "TUSS", "Radiologia"),
        new("40901050", "Densitometria óssea", "TUSS", "Radiologia"),

        // ── Procedimentos dermatológicos ───────────────────────────────────────────
        new("30701014", "Exérese de lesão de pele (até 1 cm)", "TUSS", "Dermatologia"),
        new("30701022", "Exérese de lesão de pele (1 a 3 cm)", "TUSS", "Dermatologia"),
        new("30701030", "Biópsia de pele", "TUSS", "Dermatologia"),
        new("30701049", "Crioterapia (lesão única)", "TUSS", "Dermatologia"),
        new("30701057", "Peeling químico superficial", "TUSS", "Dermatologia"),

        // ── Procedimentos ginecológicos e obstétricos ──────────────────────────────
        new("31309016", "Parto vaginal (normal)", "TUSS", "Obstetrícia"),
        new("31309024", "Cesariana", "TUSS", "Obstetrícia"),
        new("31309032", "Curetagem uterina", "TUSS", "Ginecologia"),
        new("31309040", "Histerectomia total abdominal", "TUSS", "Ginecologia"),
        new("31309059", "Laparoscopia ginecológica diagnóstica", "TUSS", "Ginecologia"),
        new("31309067", "Colposcopia", "TUSS", "Ginecologia"),

        // ── Procedimentos odontológicos (TUSS odonto) ─────────────────────────────
        new("81000010", "Exame clínico odontológico", "TUSS", "Odontologia"),
        new("81000029", "Radiografia periapical (unitária)", "TUSS", "Odontologia"),
        new("81000061", "Restauração de resina composta — face única", "TUSS", "Odontologia"),
        new("81000088", "Extração de dente permanente", "TUSS", "Odontologia"),
        new("81000096", "Raspagem e alisamento radicular (por sextante)", "TUSS", "Odontologia"),
        new("81000100", "Tratamento de canal — dente unirradicular", "TUSS", "Odontologia"),
        new("81000118", "Tratamento de canal — dente multirradicular", "TUSS", "Odontologia"),
        new("81000126", "Instalação de implante osseointegrado", "TUSS", "Odontologia"),
        new("81000134", "Coroa metálica fundida", "TUSS", "Odontologia"),
        new("81000142", "Coroa cerâmica sobre implante", "TUSS", "Odontologia"),
        new("81000150", "Aparelho ortodôntico fixo (instalação)", "TUSS", "Odontologia"),

        // ── Anestesiologia ─────────────────────────────────────────────────────────
        new("20101015", "Anestesia geral (por tempo cirúrgico)", "TUSS", "Anestesiologia"),
        new("20101023", "Anestesia regional (peridural/raqui)", "TUSS", "Anestesiologia"),
        new("20101031", "Sedação consciente", "TUSS", "Anestesiologia"),

        // ── Neurologia / Neurocirurgia ─────────────────────────────────────────────
        new("30809016", "Eletroencefalograma (EEG)", "TUSS", "Neurologia / Diagnóstico"),
        new("30809024", "Potencial evocado auditivo", "TUSS", "Neurologia / Diagnóstico"),
        new("31001016", "Descompressão de nervo periférico (nervo único)", "TUSS", "Neurocirurgia"),

        // ── Procedimentos cardiovasculares ────────────────────────────────────────
        new("30201013", "Cateterismo cardíaco diagnóstico", "TUSS", "Cardiologia / Intervenção"),
        new("30201021", "Angioplastia coronariana transluminal percutânea", "TUSS", "Cardiologia / Intervenção"),
        new("30201030", "Implante de marcapasso definitivo (câmara dupla)", "TUSS", "Cardiologia / Intervenção"),
    };
}
