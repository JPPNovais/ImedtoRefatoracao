using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imedto.Backend.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedRegioesAnatomicasCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed executado via SQL idempotente em db/migrations/20260526000001_seed_regioes_anatomicas_catalogo.sql
            // Todos os 144 registros usam ON CONFLICT (codigo) DO NOTHING.
            // Esta migration EF serve apenas como marcador no __EFMigrationsHistory.
            migrationBuilder.Sql(@"
-- ── NÍVEL 1 — Vista ANTERIOR ──────────────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('cabeca-anterior', 'Cabeça (anterior)', NULL, 1, 'anterior',
     'Crânio normocefálico, sem deformidades. Couro cabeludo sem lesões visíveis.', 1, false),
    ('pescoco-anterior', 'Pescoço (anterior)', NULL, 1, 'anterior',
     'Pescoço cilíndrico, sem massas palpáveis. Tireoide sem alterações à palpação.', 2, false),
    ('torax-anterior', 'Tórax (anterior)', NULL, 1, 'anterior',
     'Tórax simétrico, expansibilidade preservada. Murmúrio vesicular fisiológico bilateral.', 3, false),
    ('abdome-anterior', 'Abdome (anterior)', NULL, 1, 'anterior',
     'Abdome plano, flácido, indolor à palpação superficial e profunda. Ruídos hidroaéreos presentes.', 4, false),
    ('pelve-anterior', 'Pelve (anterior)', NULL, 1, 'anterior',
     'Região pélvica sem alterações à inspeção. Sem sinais de irritação peritoneal.', 5, false),
    ('msd-anterior', 'Membro superior direito (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem deformidades. Pulsos radial e ulnar palpáveis e simétricos. Força muscular preservada.', 6, false),
    ('mse-anterior', 'Membro superior esquerdo (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem deformidades. Pulsos radial e ulnar palpáveis e simétricos. Força muscular preservada.', 7, false),
    ('mid-anterior', 'Membro inferior direito (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem varizes. Pulsos pedioso e tibial posterior palpáveis. Força muscular preservada.', 8, false),
    ('mie-anterior', 'Membro inferior esquerdo (anterior)', NULL, 1, 'anterior',
     'Sem edema, sem varizes. Pulsos pedioso e tibial posterior palpáveis. Força muscular preservada.', 9, false),

-- ── NÍVEL 1 — Vista POSTERIOR ─────────────────────────────────────────────────
    ('cabeca-posterior', 'Cabeça (posterior)', NULL, 1, 'posterior',
     'Região occipital sem massas ou deformidades. Couro cabeludo sem lesões.', 10, false),
    ('pescoco-posterior', 'Pescoço (posterior)', NULL, 1, 'posterior',
     'Nuca livre. Sem rigidez de nuca. Musculatura cervical posterior sem contraturas.', 11, false),
    ('torax-posterior', 'Tórax (posterior)', NULL, 1, 'posterior',
     'Dorso simétrico. Coluna torácica sem desvios. Murmúrio vesicular fisiológico bilateral.', 12, false),
    ('lombossacra-posterior', 'Região lombossacra (posterior)', NULL, 1, 'posterior',
     'Coluna lombar sem desvios. Sem dor à palpação de processos espinhosos. Lasègue negativo bilateral.', 13, false),
    ('pelve-posterior', 'Pelve (posterior)', NULL, 1, 'posterior',
     'Região glútea sem alterações. Sem sinais de fístulas ou lesões perianais.', 14, false),
    ('msd-posterior', 'Membro superior direito (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Tríceps e musculatura posterior do antebraço com tônus preservado.', 15, false),
    ('mse-posterior', 'Membro superior esquerdo (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Tríceps e musculatura posterior do antebraço com tônus preservado.', 16, false),
    ('mid-posterior', 'Membro inferior direito (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Panturrilha sem empastamento. Sinal de Homans negativo.', 17, false),
    ('mie-posterior', 'Membro inferior esquerdo (posterior)', NULL, 1, 'posterior',
     'Face posterior sem lesões. Panturrilha sem empastamento. Sinal de Homans negativo.', 18, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 2 — Filhos de Tórax Anterior ───────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('pulmao', 'Pulmão', 'torax-anterior', 2, 'anterior',
     'Murmúrio vesicular fisiológico, sem ruídos adventícios. Expansibilidade e frêmito preservados.', 1, true),
    ('coracao', 'Coração', 'torax-anterior', 2, 'anterior',
     'Bulhas rítmicas, normofonéticas, em 2 tempos, sem sopros. Ictus cordis no 5° EIC esquerdo.', 2, false),
    ('mama', 'Mama', 'torax-anterior', 2, 'anterior',
     'Mamas simétricas, sem retrações. Sem nódulos palpáveis. Sem descarga papilar.', 3, true),
    ('axila', 'Axila', 'torax-anterior', 2, 'anterior',
     'Axilas sem linfonodomegalias palpáveis. Sem lesões cutâneas.', 5, true),
    ('parede-toracica', 'Parede torácica', 'torax-anterior', 2, 'anterior',
     'Parede torácica sem lesões ou deformidades. Sem dor à palpação.', 4, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 2 — Filhos de Abdome Anterior ──────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('epigastrio', 'Epigástrio', 'abdome-anterior', 2, 'anterior',
     'Epigástrio indolor à palpação. Sem massas palpáveis.', 1, false),
    ('mesogastrio', 'Mesogástrio', 'abdome-anterior', 2, 'anterior',
     'Mesogástrio indolor. Sem visceromegalias.', 2, false),
    ('hipogastrio', 'Hipogástrio', 'abdome-anterior', 2, 'anterior',
     'Hipogástrio indolor. Sem massas palpáveis. Bexiga não palpável.', 3, false),
    ('hipocondrio', 'Hipocôndrio', 'abdome-anterior', 2, 'anterior',
     'Hipocôndrio indolor à palpação superficial e profunda.', 4, true),
    ('flanco', 'Flanco', 'abdome-anterior', 2, 'anterior',
     'Flancos indolores. Sem sinais de irritação peritoneal.', 5, true),
    ('fossa-iliaca', 'Fossa ilíaca', 'abdome-anterior', 2, 'anterior',
     'Fossa ilíaca indolor à palpação. Sem massas ou hérnias.', 6, true),
    ('figado', 'Fígado', 'abdome-anterior', 2, 'anterior',
     'Fígado não palpável abaixo do rebordo costal. Espaço de Traube livre.', 7, false),
    ('baco', 'Baço', 'abdome-anterior', 2, 'anterior',
     'Baço não palpável. Espaço de Traube livre.', 8, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 2 — Filhos de Pelve Anterior ───────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('regiao-inguinal', 'Região inguinal', 'pelve-anterior', 2, 'anterior',
     'Região inguinal sem abaulamentos. Sem hérnias. Linfonodos inguinais não palpáveis.', 1, true),
    ('genitalia', 'Genitália', 'pelve-anterior', 2, 'anterior',
     'Genitália externa sem alterações à inspeção.', 2, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 2 — Filhos de Cabeça Anterior ──────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('olho', 'Olho', 'cabeca-anterior', 2, 'anterior',
     'Pupilas isocóricas e fotorreagentes. Conjuntivas normocoradas. Escleras anictéricas.', 1, true),
    ('orelha', 'Orelha', 'cabeca-anterior', 2, 'anterior',
     'Pavilhão auricular sem alterações. Meato acústico externo pérvio.', 2, true),
    ('nariz', 'Nariz', 'cabeca-anterior', 2, 'anterior',
     'Pirâmide nasal sem desvios. Mucosa nasal úmida e normocorada. Septo centrado.', 3, false),
    ('boca', 'Boca', 'cabeca-anterior', 2, 'anterior',
     'Mucosa oral úmida e normocorada. Orofaringe sem hiperemia. Amígdalas sem hipertrofia.', 4, false),
    ('face', 'Face', 'cabeca-anterior', 2, 'anterior',
     'Facies atípica. Sem assimetrias. Sem lesões cutâneas visíveis.', 5, false),
    ('fronte', 'Fronte', 'cabeca-anterior', 2, 'anterior',
     'Fronte sem abaulamentos ou depressões. Pele íntegra.', 6, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 2 — Filhos de Pescoço Anterior ─────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('tireoide', 'Tireoide', 'pescoco-anterior', 2, 'anterior',
     'Tireoide de tamanho normal à palpação, sem nódulos. Indolor.', 1, false),
    ('linfonodos-cervicais', 'Linfonodos cervicais', 'pescoco-anterior', 2, 'anterior',
     'Linfonodos cervicais não palpáveis bilateralmente.', 2, true),
    ('carotidas', 'Carótidas', 'pescoco-anterior', 2, 'anterior',
     'Pulsos carotídeos simétricos, sem sopros.', 3, true),
    ('jugulares', 'Jugulares', 'pescoco-anterior', 2, 'anterior',
     'Veias jugulares sem ingurgitamento com paciente a 45°.', 4, true)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 2 — Filhos de MSD Anterior ─────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('ombro-direito', 'Ombro direito', 'msd-anterior', 2, 'anterior',
     'Ombro sem deformidades. Amplitude de movimento preservada.', 1, false),
    ('braco-direito', 'Braço direito', 'msd-anterior', 2, 'anterior',
     'Braço sem edema ou lesões. Musculatura com tônus preservado.', 2, false),
    ('cotovelo-direito', 'Cotovelo direito', 'msd-anterior', 2, 'anterior',
     'Cotovelo sem derrame articular. Flexo-extensão completa.', 3, false),
    ('antebraco-direito', 'Antebraço direito', 'msd-anterior', 2, 'anterior',
     'Antebraço sem edema. Prono-supinação preservada.', 4, false),
    ('punho-direito', 'Punho direito', 'msd-anterior', 2, 'anterior',
     'Punho sem edema ou deformidades. Teste de Phalen e Tinel negativos.', 5, false),
    ('mao-direita', 'Mão direita', 'msd-anterior', 2, 'anterior',
     'Mão sem edema ou deformidades. Preensão preservada. Perfusão capilar < 3s.', 6, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 2 — Filhos de MSE Anterior ─────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('ombro-esquerdo', 'Ombro esquerdo', 'mse-anterior', 2, 'anterior',
     'Ombro sem deformidades. Amplitude de movimento preservada.', 1, false),
    ('braco-esquerdo', 'Braço esquerdo', 'mse-anterior', 2, 'anterior',
     'Braço sem edema ou lesões. Musculatura com tônus preservado.', 2, false),
    ('cotovelo-esquerdo', 'Cotovelo esquerdo', 'mse-anterior', 2, 'anterior',
     'Cotovelo sem derrame articular. Flexo-extensão completa.', 3, false),
    ('antebraco-esquerdo', 'Antebraço esquerdo', 'mse-anterior', 2, 'anterior',
     'Antebraço sem edema. Prono-supinação preservada.', 4, false),
    ('punho-esquerdo', 'Punho esquerdo', 'mse-anterior', 2, 'anterior',
     'Punho sem edema ou deformidades. Teste de Phalen e Tinel negativos.', 5, false),
    ('mao-esquerda', 'Mão esquerda', 'mse-anterior', 2, 'anterior',
     'Mão sem edema ou deformidades. Preensão preservada. Perfusão capilar < 3s.', 6, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 2 — Filhos de MID Anterior ─────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('quadril-direito', 'Quadril direito', 'mid-anterior', 2, 'anterior',
     'Quadril sem deformidades. Amplitude de movimento preservada.', 1, false),
    ('coxa-direita', 'Coxa direita', 'mid-anterior', 2, 'anterior',
     'Coxa sem edema ou lesões. Musculatura com tônus e trofismo preservados.', 2, false),
    ('joelho-direito', 'Joelho direito', 'mid-anterior', 2, 'anterior',
     'Joelho sem derrame articular. Ligamentos estáveis. Flexo-extensão completa.', 3, false),
    ('perna-direita', 'Perna direita', 'mid-anterior', 2, 'anterior',
     'Perna sem edema. Sem varizes. Sem sinais de TVP.', 4, false),
    ('tornozelo-direito', 'Tornozelo direito', 'mid-anterior', 2, 'anterior',
     'Tornozelo sem edema ou instabilidade.', 5, false),
    ('pe-direito', 'Pé direito', 'mid-anterior', 2, 'anterior',
     'Pé sem deformidades. Arco plantar preservado. Pulso pedioso palpável.', 6, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 2 — Filhos de MIE Anterior ─────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('quadril-esquerdo', 'Quadril esquerdo', 'mie-anterior', 2, 'anterior',
     'Quadril sem deformidades. Amplitude de movimento preservada.', 1, false),
    ('coxa-esquerda', 'Coxa esquerda', 'mie-anterior', 2, 'anterior',
     'Coxa sem edema ou lesões. Musculatura com tônus e trofismo preservados.', 2, false),
    ('joelho-esquerdo', 'Joelho esquerdo', 'mie-anterior', 2, 'anterior',
     'Joelho sem derrame articular. Ligamentos estáveis. Flexo-extensão completa.', 3, false),
    ('perna-esquerda', 'Perna esquerda', 'mie-anterior', 2, 'anterior',
     'Perna sem edema. Sem varizes. Sem sinais de TVP.', 4, false),
    ('tornozelo-esquerdo', 'Tornozelo esquerdo', 'mie-anterior', 2, 'anterior',
     'Tornozelo sem edema ou instabilidade.', 5, false),
    ('pe-esquerdo', 'Pé esquerdo', 'mie-anterior', 2, 'anterior',
     'Pé sem deformidades. Arco plantar preservado. Pulso pedioso palpável.', 6, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 2 — Vista POSTERIOR ─────────────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('occipital', 'Occipital', 'cabeca-posterior', 2, 'posterior',
     'Região occipital sem massas ou pontos dolorosos.', 1, false),
    ('mastoide', 'Mastóide', 'cabeca-posterior', 2, 'posterior',
     'Região mastoidea sem dor à palpação. Sem sinais de Battle.', 2, true),
    ('musculatura-cervical', 'Musculatura cervical', 'pescoco-posterior', 2, 'posterior',
     'Musculatura cervical posterior sem contraturas. Sem pontos gatilho.', 1, false),
    ('linfonodos-cervicais-post', 'Linfonodos cervicais posteriores', 'pescoco-posterior', 2, 'posterior',
     'Linfonodos cervicais posteriores não palpáveis.', 2, true),
    ('coluna-toracica', 'Coluna torácica', 'torax-posterior', 2, 'posterior',
     'Coluna torácica alinhada, sem desvios.', 1, false),
    ('escapula', 'Escápula', 'torax-posterior', 2, 'posterior',
     'Escápula sem alamento. Musculatura periescapular com tônus preservado.', 2, true),
    ('pulmao-posterior', 'Pulmão (posterior)', 'torax-posterior', 2, 'posterior',
     'Murmúrio vesicular fisiológico bilateralmente na ausculta posterior.', 3, true),
    ('coluna-lombar', 'Coluna lombar', 'lombossacra-posterior', 2, 'posterior',
     'Coluna lombar alinhada. Sem dor à palpação. Mobilidade preservada.', 1, false),
    ('regiao-sacral', 'Região sacral', 'lombossacra-posterior', 2, 'posterior',
     'Região sacral sem dor à palpação. Sem fístulas ou lesões.', 2, false),
    ('musculatura-paravertebral', 'Musculatura paravertebral', 'lombossacra-posterior', 2, 'posterior',
     'Musculatura paravertebral sem contraturas ou pontos dolorosos.', 3, true),
    ('gluteo', 'Glúteo', 'pelve-posterior', 2, 'posterior',
     'Região glútea sem alterações. Musculatura com tônus preservado.', 1, true),
    ('regiao-perianal', 'Região perianal', 'pelve-posterior', 2, 'posterior',
     'Região perianal sem fissuras, fístulas ou hemorroidas externas.', 2, false),
    ('ombro-direito-post', 'Ombro direito (posterior)', 'msd-posterior', 2, 'posterior',
     'Face posterior do ombro sem alterações.', 1, false),
    ('braco-direito-post', 'Braço direito (posterior)', 'msd-posterior', 2, 'posterior',
     'Face posterior do braço sem lesões. Tríceps com tônus preservado.', 2, false),
    ('cotovelo-direito-post', 'Cotovelo direito (posterior)', 'msd-posterior', 2, 'posterior',
     'Olécrano sem bursite.', 3, false),
    ('ombro-esquerdo-post', 'Ombro esquerdo (posterior)', 'mse-posterior', 2, 'posterior',
     'Face posterior do ombro sem alterações.', 1, false),
    ('braco-esquerdo-post', 'Braço esquerdo (posterior)', 'mse-posterior', 2, 'posterior',
     'Face posterior do braço sem lesões. Tríceps com tônus preservado.', 2, false),
    ('cotovelo-esquerdo-post', 'Cotovelo esquerdo (posterior)', 'mse-posterior', 2, 'posterior',
     'Olécrano sem bursite.', 3, false),
    ('coxa-direita-post', 'Coxa direita (posterior)', 'mid-posterior', 2, 'posterior',
     'Face posterior da coxa sem lesões. Isquiotibiais com tônus preservado.', 1, false),
    ('fossa-poplitea-direita', 'Fossa poplítea direita', 'mid-posterior', 2, 'posterior',
     'Fossa poplítea sem massas palpáveis. Sem cisto de Baker.', 2, false),
    ('panturrilha-direita', 'Panturrilha direita', 'mid-posterior', 2, 'posterior',
     'Panturrilha sem empastamento. Sinal de Homans negativo.', 3, false),
    ('tendao-aquiles-direito', 'Tendão de Aquiles direito', 'mid-posterior', 2, 'posterior',
     'Tendão de Aquiles sem espessamento. Thompson negativo.', 4, false),
    ('coxa-esquerda-post', 'Coxa esquerda (posterior)', 'mie-posterior', 2, 'posterior',
     'Face posterior da coxa sem lesões. Isquiotibiais com tônus preservado.', 1, false),
    ('fossa-poplitea-esquerda', 'Fossa poplítea esquerda', 'mie-posterior', 2, 'posterior',
     'Fossa poplítea sem massas palpáveis. Sem cisto de Baker.', 2, false),
    ('panturrilha-esquerda', 'Panturrilha esquerda', 'mie-posterior', 2, 'posterior',
     'Panturrilha sem empastamento. Sinal de Homans negativo.', 3, false),
    ('tendao-aquiles-esquerdo', 'Tendão de Aquiles esquerdo', 'mie-posterior', 2, 'posterior',
     'Tendão de Aquiles sem espessamento. Thompson negativo.', 4, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Pulmão ────────────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('apice-pulmonar', 'Ápice pulmonar', 'pulmao', 3, 'anterior',
     'MV fisiológico no ápice. Sem ruídos adventícios.', 1, true),
    ('base-pulmonar', 'Base pulmonar', 'pulmao', 3, 'anterior',
     'MV fisiológico na base. Sem crepitações ou sibilos.', 2, true),
    ('lobo-medio-lingula', 'Lobo médio/língula', 'pulmao', 3, 'anterior',
     'Ausculta sem alterações em lobo médio/língula.', 3, true)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Coração ───────────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('foco-aortico', 'Foco aórtico', 'coracao', 3, 'anterior',
     'Bulha normofonética. Sem sopros.', 1, false),
    ('foco-pulmonar', 'Foco pulmonar', 'coracao', 3, 'anterior',
     'Bulha normofonética. Sem sopros.', 2, false),
    ('foco-tricuspide', 'Foco tricúspide', 'coracao', 3, 'anterior',
     'Bulha normofonética. Sem sopros.', 3, false),
    ('foco-mitral', 'Foco mitral', 'coracao', 3, 'anterior',
     'Bulha normofonética. Sem sopros. Sem cliques.', 4, false),
    ('foco-aortico-acessorio', 'Foco aórtico acessório', 'coracao', 3, 'anterior',
     'Sem sopros (ponto de Erb).', 5, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Mama ──────────────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('qse-mama', 'QSE mama', 'mama', 3, 'anterior',
     'Quadrante superior externo sem nódulos.', 1, true),
    ('qsi-mama', 'QSI mama', 'mama', 3, 'anterior',
     'Quadrante superior interno sem nódulos.', 2, true),
    ('qie-mama', 'QIE mama', 'mama', 3, 'anterior',
     'Quadrante inferior externo sem nódulos.', 3, true),
    ('qii-mama', 'QII mama', 'mama', 3, 'anterior',
     'Quadrante inferior interno sem nódulos.', 4, true),
    ('regiao-areolar', 'Região areolar', 'mama', 3, 'anterior',
     'Sem retração ou descarga papilar.', 5, true)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Olho ──────────────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('palpebra', 'Pálpebra', 'olho', 3, 'anterior',
     'Pálpebras sem ptose, edema ou lesões.', 1, true),
    ('conjuntiva', 'Conjuntiva', 'olho', 3, 'anterior',
     'Conjuntiva normocorada e sem hiperemia.', 2, true),
    ('cornea', 'Córnea', 'olho', 3, 'anterior',
     'Córnea transparente, sem opacidades.', 3, true),
    ('pupila', 'Pupila', 'olho', 3, 'anterior',
     'Pupilas isocóricas, fotorreagentes.', 4, true),
    ('fundo-de-olho', 'Fundo de olho', 'olho', 3, 'anterior',
     'Papila de bordos nítidos. Vasos sem alterações.', 5, true)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Orelha ────────────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('pavilhao-auricular', 'Pavilhão auricular', 'orelha', 3, 'anterior',
     'Pavilhão auricular sem deformidades ou lesões.', 1, true),
    ('conduto-auditivo', 'Conduto auditivo', 'orelha', 3, 'anterior',
     'Conduto auditivo externo pérvio.', 2, true),
    ('membrana-timpanica', 'Membrana timpânica', 'orelha', 3, 'anterior',
     'Membrana timpânica íntegra, translúcida.', 3, true)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Boca ──────────────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('labios', 'Lábios', 'boca', 3, 'anterior',
     'Lábios normocorados, sem lesões.', 1, false),
    ('lingua', 'Língua', 'boca', 3, 'anterior',
     'Língua normocorada, úmida, sem saburra.', 2, false),
    ('gengiva', 'Gengiva', 'boca', 3, 'anterior',
     'Gengiva normocorada, sem hipertrofia.', 3, false),
    ('palato', 'Palato', 'boca', 3, 'anterior',
     'Palato duro e mole sem lesões. Úvula centrada.', 4, false),
    ('orofaringe', 'Orofaringe', 'boca', 3, 'anterior',
     'Orofaringe sem hiperemia. Amígdalas sem hipertrofia.', 5, false),
    ('dentes', 'Dentes', 'boca', 3, 'anterior',
     'Arcada dentária sem cáries visíveis.', 6, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Joelho Direito ───────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('menisco-medial-d', 'Menisco medial D', 'joelho-direito', 3, 'anterior',
     'McMurray negativo para menisco medial.', 1, false),
    ('menisco-lateral-d', 'Menisco lateral D', 'joelho-direito', 3, 'anterior',
     'McMurray negativo para menisco lateral.', 2, false),
    ('lca-direito', 'LCA direito', 'joelho-direito', 3, 'anterior',
     'Lachman negativo. Gaveta anterior negativa.', 3, false),
    ('lcp-direito', 'LCP direito', 'joelho-direito', 3, 'anterior',
     'Gaveta posterior negativa.', 4, false),
    ('patela-d', 'Patela D', 'joelho-direito', 3, 'anterior',
     'Patela centrada, sem derrame.', 5, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Joelho Esquerdo ──────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('menisco-medial-e', 'Menisco medial E', 'joelho-esquerdo', 3, 'anterior',
     'McMurray negativo para menisco medial.', 1, false),
    ('menisco-lateral-e', 'Menisco lateral E', 'joelho-esquerdo', 3, 'anterior',
     'McMurray negativo para menisco lateral.', 2, false),
    ('lca-esquerdo', 'LCA esquerdo', 'joelho-esquerdo', 3, 'anterior',
     'Lachman negativo. Gaveta anterior negativa.', 3, false),
    ('lcp-esquerdo', 'LCP esquerdo', 'joelho-esquerdo', 3, 'anterior',
     'Gaveta posterior negativa.', 4, false),
    ('patela-e', 'Patela E', 'joelho-esquerdo', 3, 'anterior',
     'Patela centrada, sem derrame.', 5, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Mão Direita ──────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('dedos-mao-d', 'Dedos mão D', 'mao-direita', 3, 'anterior',
     'Sem deformidades. Mobilidade preservada.', 1, false),
    ('mcf-direitas', 'MCF direitas', 'mao-direita', 3, 'anterior',
     'Sem edema, dor ou desvio ulnar.', 2, false),
    ('eminencia-tenar-d', 'Eminência tenar D', 'mao-direita', 3, 'anterior',
     'Sem atrofia. Oposição preservada.', 3, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Mão Esquerda ─────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('dedos-mao-e', 'Dedos mão E', 'mao-esquerda', 3, 'anterior',
     'Sem deformidades. Mobilidade preservada.', 1, false),
    ('mcf-esquerdas', 'MCF esquerdas', 'mao-esquerda', 3, 'anterior',
     'Sem edema, dor ou desvio ulnar.', 2, false),
    ('eminencia-tenar-e', 'Eminência tenar E', 'mao-esquerda', 3, 'anterior',
     'Sem atrofia. Oposição preservada.', 3, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Pé Direito ───────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('dedos-pe-d', 'Dedos pé D', 'pe-direito', 3, 'anterior',
     'Sem deformidades. Unhas sem alterações.', 1, false),
    ('arco-plantar-d', 'Arco plantar D', 'pe-direito', 3, 'anterior',
     'Arco plantar preservado.', 2, false),
    ('calcanhar-d', 'Calcanhar D', 'pe-direito', 3, 'anterior',
     'Sem esporão palpável.', 3, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Pé Esquerdo ──────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('dedos-pe-e', 'Dedos pé E', 'pe-esquerdo', 3, 'anterior',
     'Sem deformidades. Unhas sem alterações.', 1, false),
    ('arco-plantar-e', 'Arco plantar E', 'pe-esquerdo', 3, 'anterior',
     'Arco plantar preservado.', 2, false),
    ('calcanhar-e', 'Calcanhar E', 'pe-esquerdo', 3, 'anterior',
     'Sem esporão palpável.', 3, false)
ON CONFLICT (codigo) DO NOTHING;

-- ── NÍVEL 3 — Filhos de Coluna Lombar ────────────────────────────────────────
INSERT INTO public.regioes_anatomicas_catalogo
    (codigo, nome, pai_codigo, nivel, vista, template_texto, ordem, lateralidade)
VALUES
    ('articulacao-sacroiliaca', 'Articulação sacroilíaca', 'coluna-lombar', 3, 'posterior',
     'Sem dor. Patrick/FABER negativo.', 1, false),
    ('processos-espinhosos-lombares', 'Processos espinhosos lombares', 'coluna-lombar', 3, 'posterior',
     'Sem dor à palpação. Sem degraus palpáveis.', 2, false)
ON CONFLICT (codigo) DO NOTHING;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Seed reverso: remove apenas os registros inseridos por esta migration.
            // Seguro porque ON CONFLICT DO NOTHING garante que nenhum dado pré-existente foi alterado.
            migrationBuilder.Sql(@"
DELETE FROM public.regioes_anatomicas_catalogo
WHERE codigo IN (
    'cabeca-anterior','pescoco-anterior','torax-anterior','abdome-anterior',
    'pelve-anterior','msd-anterior','mse-anterior','mid-anterior','mie-anterior',
    'cabeca-posterior','pescoco-posterior','torax-posterior','lombossacra-posterior',
    'pelve-posterior','msd-posterior','mse-posterior','mid-posterior','mie-posterior',
    'pulmao','coracao','mama','axila','parede-toracica',
    'epigastrio','mesogastrio','hipogastrio','hipocondrio','flanco','fossa-iliaca','figado','baco',
    'regiao-inguinal','genitalia',
    'olho','orelha','nariz','boca','face','fronte',
    'tireoide','linfonodos-cervicais','carotidas','jugulares',
    'ombro-direito','braco-direito','cotovelo-direito','antebraco-direito','punho-direito','mao-direita',
    'ombro-esquerdo','braco-esquerdo','cotovelo-esquerdo','antebraco-esquerdo','punho-esquerdo','mao-esquerda',
    'quadril-direito','coxa-direita','joelho-direito','perna-direita','tornozelo-direito','pe-direito',
    'quadril-esquerdo','coxa-esquerda','joelho-esquerdo','perna-esquerda','tornozelo-esquerdo','pe-esquerdo',
    'occipital','mastoide','musculatura-cervical','linfonodos-cervicais-post',
    'coluna-toracica','escapula','pulmao-posterior',
    'coluna-lombar','regiao-sacral','musculatura-paravertebral',
    'gluteo','regiao-perianal',
    'ombro-direito-post','braco-direito-post','cotovelo-direito-post',
    'ombro-esquerdo-post','braco-esquerdo-post','cotovelo-esquerdo-post',
    'coxa-direita-post','fossa-poplitea-direita','panturrilha-direita','tendao-aquiles-direito',
    'coxa-esquerda-post','fossa-poplitea-esquerda','panturrilha-esquerda','tendao-aquiles-esquerdo',
    'apice-pulmonar','base-pulmonar','lobo-medio-lingula',
    'foco-aortico','foco-pulmonar','foco-tricuspide','foco-mitral','foco-aortico-acessorio',
    'qse-mama','qsi-mama','qie-mama','qii-mama','regiao-areolar',
    'palpebra','conjuntiva','cornea','pupila','fundo-de-olho',
    'pavilhao-auricular','conduto-auditivo','membrana-timpanica',
    'labios','lingua','gengiva','palato','orofaringe','dentes',
    'menisco-medial-d','menisco-lateral-d','lca-direito','lcp-direito','patela-d',
    'menisco-medial-e','menisco-lateral-e','lca-esquerdo','lcp-esquerdo','patela-e',
    'dedos-mao-d','mcf-direitas','eminencia-tenar-d',
    'dedos-mao-e','mcf-esquerdas','eminencia-tenar-e',
    'dedos-pe-d','arco-plantar-d','calcanhar-d',
    'dedos-pe-e','arco-plantar-e','calcanhar-e',
    'articulacao-sacroiliaca','processos-espinhosos-lombares'
);
");
        }
    }
}
