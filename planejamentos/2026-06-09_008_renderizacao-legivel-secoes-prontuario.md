# Renderização legível das seções estruturadas no drawer "Ver" e no PDF de evolução

**ID**: 2026-06-09_008
**Status**: Aprovado por usuário em 2026-06-09
**Autor**: imedto-business-analyst
**Estimativa de esforço**: M
**Áreas regressivas tocadas**: prontuário (leitura de evolução), relatório (PDF de evolução). Nenhuma mudança de escrita/schema.

## 1. Contexto e motivação

Hoje, no prontuário do paciente, a modal "Ver" (`EvolucaoDetalheDrawer`) e o PDF de evolução (`useProntuarioPdf`) renderizam o conteúdo das seções estruturadas (HPP, História familiar, História social, Exame físico, Exames realizados, Procedimentos indicados) em formato técnico `chave: valor`, copiado direto do JSON salvo em `conteudo[chave]`.

Resultado atual para o profissional que lê:

```
alergiasTem: true
alergias: nome: TESTEE
observacao:
medicacoesTem: false
```

Isso expõe nome de campo técnico (`alergiasTem`, `nome`), valores booleanos crus (`true`/`false`), chaves de flags de controle e campos vazios (`observacao:`). É ruído ilegível para uso clínico e degrada o PDF que o paciente/profissional recebe.

O ponto único de formatação compartilhado por modal e PDF é `valorSecaoParaTexto(v: unknown)` em `frontend/src/composables/useEvolucaoResumo.ts`. Ele faz fallback genérico recursivo (`chave: valor` por linha), sem conhecimento dos rótulos clínicos nem da ordem de campos esperada. É exatamente esse ponto que precisa evoluir.

**Objetivo**: renderização LEGÍVEL de todos os campos, em AMBOS (modal e PDF), com PARIDADE total — a mesma string nos dois canais. Exemplo esperado para HPP: `Alergias: TESTEE` (sem `alergiasTem`, sem `nome:` técnico, sem `observacao` vazia).

Esta é uma demanda de apresentação/leitura: não altera o que é salvo, não altera schema, não cria endpoint. Só reorganiza e humaniza o que já é exibido hoje.

## 2. Persona-alvo

Profissional de saúde (e, no PDF, também o paciente) lendo uma evolução já registrada — na modal de leitura do prontuário e no PDF de evolução exportado. Acontece a cada consulta de retorno, revisão de histórico e entrega de documento. Alta frequência.

## 3. Escopo

**Inclui**:
- Substituir o formatador genérico `valorSecaoParaTexto(v)` por um formatador que conhece a `chave` da seção: `formatarSecaoLegivel(chave: string, valor: unknown): string`.
- Formatadores curados (rótulos clínicos + ordem de campos) para as 6 seções estruturadas conhecidas: `hpp`, `h-familiar`, `h-social`, `exame-fisico`, `exames-realizados`, `procedimentos-indicados`.
- Fallback genérico para qualquer outra `chave`/estrutura não mapeada (humaniza camelCase, sem expor `true`/`false` cru nem chave técnica).
- Atualizar os dois consumidores (modal `EvolucaoDetalheDrawer.vue` e PDF `useProntuarioPdf.ts`, ambas as funções `gerarPdf` e `gerarPdfEvolucao`) para passar a `chave` ao formatador.
- Manter PARIDADE: modal e PDF produzem exatamente a mesma string por seção.

**Não inclui**:
- Qualquer mudança no formato salvo em `conteudo`, no schema, em migration ou endpoint.
- Mudança na lógica de edição das seções (`secoes/*.vue` permanecem intocadas).
- Novo audit trail (o drawer é sem-audit por decisão do briefing 2026-05-25_001; LGPD não muda — ver R8).
- Renderização rica (HTML/markdown/cores) dentro do texto da seção — a saída continua sendo string de texto, com quebras de linha. (Campo Exame é textarea sem markdown — ver memória do projeto.)
- Reordenar ou renomear seções (os títulos vêm de `modeloSnapshot[].titulo`, inalterados).

## 4. Regras de negócio

Todas as regras desta demanda vivem no **Front** (camada de apresentação), pois é formatação de leitura de dado já persistido. Não há espelho no back — nenhuma regra de negócio nova é introduzida; o backend não muda. O "ponto único de verdade" é `formatarSecaoLegivel` em `useEvolucaoResumo.ts`.

- **R1 — Abordagem híbrida.** `formatarSecaoLegivel(chave, valor)` despacha para um formatador curado quando `chave ∈ {hpp, h-familiar, h-social, exame-fisico, exames-realizados, procedimentos-indicados}`; caso contrário usa o fallback genérico. Mora em: `useEvolucaoResumo.ts`.

- **R2 — Fallback genérico nunca expõe ruído técnico.** Para chave/estrutura não mapeada: humaniza a chave camelCase em rótulo legível (`atividadeFisicaNivel` → `Atividade física nível` ou equivalente legível), NUNCA imprime `true`/`false` cru nem o nome técnico da chave como aparece no JSON. Booleano vira rótulo sem o sufixo de flag, ou é omitido se não houver conteúdo associado. String/número aparecem com seu rótulo humanizado. Campos vazios são omitidos.

- **R3 — Flags negativas (`*Tem === false`) registram a negativa SOMENTE para itens clínicos relevantes.** São eles: alergias, medicações (HPP), tabagismo, etilismo, drogas (História social). Saída: `Alergias: Nega`, `Medicações de uso: Nega`, `Tabagismo: Não`, `Etilismo: Não`, `Drogas: Não` (rótulo/texto exato definido na nota de execução §9). Todas as demais flags negativas (ex: `cirurgiasTem=false`, `doencasTem=false`, `filhosTem=false`, `atividadeFisicaTem=false`) e itens vazios são OMITIDOS.

- **R4 — Flag positiva sem conteúdo é omitida.** Se `*Tem === true` mas a lista/campos correspondentes estão vazios (sem item legível), a subseção NÃO aparece (nem rótulo, nem "Sim" solto). Mora em: cada formatador curado.

- **R5 — Arrays de objetos: campo principal + detalhes presentes.** Cada item vira uma linha: campo principal seguido dos detalhes preenchidos entre parênteses ou após travessão, conforme o tipo. Campos vazios do item são omitidos. Formatos canônicos (ver §9 para a forma exata):
  - Alergia: `Dipirona (reação leve)` — sem observação: `Dipirona`.
  - Medicação: `Losartana 50mg, 1x/dia — Hipertensão` — só os campos preenchidos entre `nome`, `dose`, `frequencia`, `motivo`, `observacoes`.
  - Cirurgia: `Apendicectomia (2015)` — sem ano: `Apendicectomia`; com observação adicional.
  - Doença prévia: `Hipertensão (controlada)`.
  - Parente (h-familiar): `Avó materna: Diabetes — comentário`.
  - Exame realizado: `Hemograma (Laboratorial, sangue) — comentário`.
  - Procedimento indicado: `Infiltração — observação`.

- **R6 — Exame físico: sinais vitais agrupados + regiões detalhadas.**
  - Sinais vitais consolidados numa linha única, só os campos preenchidos, com unidades: `PA: 120/80 mmHg, FC: 72 bpm, FR: 18 irpm, Temp: 36.5 °C, SpO₂: 98%, Glicemia: 95 mg/dL`. PA só aparece se ao menos uma das pressões (`paSistolica`/`paDiastolica`) estiver preenchida.
  - Antropometria: `Peso: 80 kg, Altura: 1.75 m` (só preenchidos). IMC NÃO precisa ser recalculado para o texto (não está salvo); se ausente, omite.
  - Ectoscopia: campos de select preenchidos + `descricaoEctoscopia` quando houver.
  - Cada região anatômica vira uma linha com local + lateralidade + achados/texto: `Tórax (anterior), bilateral: <texto_exame ou achados>`. Lateralidade traduzida: `D` → "direito/direita" (ou "lado direito"), `E` → "esquerdo", `bilateral` → "bilateral", `misto` → "misto", `null` → omitido. `observacoesExame` (geral) aparece ao final quando preenchido.
  - Mora em: formatador curado de `exame-fisico` em `useEvolucaoResumo.ts`.

- **R7 — Seção que resulta vazia após formatação é OMITIDA.** Se o resultado de `formatarSecaoLegivel(chave, valor)` for string vazia (`""` após `trim`), a seção não aparece na modal nem no PDF. A decisão de exibir/omitir cada seção deve passar a basear-se no resultado do formatador (não em heurística independente do drawer), para garantir paridade — ver R9.

- **R8 — LGPD inalterada.** A formatação não expõe nenhum campo além dos já exibidos hoje (todos os campos de `conteudo[chave]` já eram impressos pelo fallback genérico). Só reorganiza/humaniza. Sem novo audit (drawer já é sem-audit por decisão do briefing 2026-05-25_001). Sem PII em mensagem de erro. Mora em: Front (premissa de design).

- **R9 — Paridade modal ↔ PDF.** Modal e PDF DEVEM produzir a mesma string por seção, porque consomem o mesmo `formatarSecaoLegivel(chave, valor)`. Tanto a decisão de exibir a seção quanto o texto exibido derivam da mesma função. Mora em: `useEvolucaoResumo.ts` (fonte única) consumido por `EvolucaoDetalheDrawer.vue` e `useProntuarioPdf.ts`.

- **R10 — Seção de texto puro inalterada.** Quando `conteudo[chave]` é string (seções de texto livre como queixa, conduta), a saída é o próprio texto trimado, como hoje. O fallback string atual é preservado.

## 5. Modelo de dados

Nenhuma alteração. Sem tabela nova, sem coluna nova, sem índice, sem migration. Apenas leitura do JSON `conteudo` da evolução (`Evolucao.conteudo[chave]`) e do `modeloSnapshot` já existentes. Vínculo multi-tenant e audit permanecem como hoje (sem mudança).

## 6. UX e fluxo

- **Modal "Ver" (`EvolucaoDetalheDrawer`)**: cada seção preenchida exibe `secao.titulo` (de `modeloSnapshot`) seguido do texto legível em `<p class="edd-secao-conteudo">`. Quebras de linha (`\n`) entre itens devem ser renderizadas (já há tratamento `white-space`/`pre-line` no conteúdo; o dev confirma e mantém). Estados: a modal já só lista seções preenchidas; após esta mudança, a lista de seções exibidas usa o resultado do formatador (R7/R9). Sem mudança de layout, drawer, ou componentes do design system.
- **PDF (`useProntuarioPdf`, `gerarPdf` e `gerarPdfEvolucao`)**: cada seção vira bloco `{ titulo, valor }` onde `valor` é o texto legível. Seções com `valor` vazio são omitidas (já filtra hoje; manter). Layout, fonte (Nunito) e header (`usePdfHeader.ts`) inalterados.
- Não há novo componente, novo estado de loading/erro/vazio nem atalho de teclado. Mobile e responsivo inalterados.

## 7. Critérios de aceite (testáveis)

- **CA1 — HPP com alergia preenchida (caminho feliz).** Dado uma evolução com `conteudo.hpp = { alergiasTem: true, alergias: [{ nome: "TESTEE", observacao: "" }] }`, Quando a seção HPP é renderizada na modal, Então o texto contém `Alergias: TESTEE` e NÃO contém as strings `alergiasTem`, `nome:`, `observacao`, `true`.

- **CA2 — HPP negativa clínica registrada.** Dado `conteudo.hpp = { alergiasTem: false, medicacoesTem: false }`, Quando a seção HPP é renderizada, Então aparece `Alergias: Nega` e `Medicações de uso: Nega` (rótulos exatos conforme §9), e NÃO aparece nenhuma das strings `false`, `alergiasTem`, `medicacoesTem`.

- **CA3 — HPP flags negativas não-clínicas omitidas.** Dado `conteudo.hpp = { cirurgiasTem: false, doencasTem: false }`, Quando renderizada, Então nem "Cirurgias" nem "Doenças" aparecem (negativa só vale para alergias/medicações/tabagismo/etilismo/drogas — R3), e o resultado da seção é string vazia → a seção HPP é OMITIDA por completo (R7).

- **CA4 — HPP flag positiva sem itens é omitida.** Dado `conteudo.hpp = { cirurgiasTem: true, cirurgias: [{ nome: "", ano: "", observacao: "" }] }`, Quando renderizada, Então a subseção Cirurgias NÃO aparece (sem item legível — R4) e não imprime `Sim` solto.

- **CA5 — Array de medicação com detalhes parciais.** Dado `medicacoes: [{ nome: "Losartana", dose: "50mg", frequencia: "1x/dia", motivo: "Hipertensão", observacoes: "" }]` com `medicacoesTem: true`, Quando renderizada, Então a linha é `Losartana 50mg, 1x/dia — Hipertensão` (campo `observacoes` vazio omitido, sem rótulos técnicos `dose:`/`motivo:`).

- **CA6 — Cirurgia sem ano.** Dado `cirurgias: [{ nome: "Apendicectomia", ano: "", observacao: "" }]` com `cirurgiasTem: true`, Quando renderizada, Então a linha é `Apendicectomia` (sem parênteses vazios, sem `(/)`).

- **CA7 — História familiar com pai/mãe/parente.** Dado `conteudo["h-familiar"] = { paiDoencas: "Hipertensão", paiDescricao: "", maeDoencas: "", parentes: [{ parentesco: "Avó materna", doencas: "Diabetes", comentario: "tipo 2" }], observacao: "" }`, Quando renderizada, Então aparece `Pai: Hipertensão` e `Avó materna: Diabetes — tipo 2`, e NÃO aparecem `paiDoencas`, `maeDoencas` (vazio omitido), `parentesco:` técnico.

- **CA8 — História social com negativas e positivas mistas.** Dado `conteudo["h-social"] = { estadoCivil: "Casado(a)", tabagismoTem: false, etilismoTem: true, etilismoStatus: "Social", drogasTem: false, atividadeFisicaTem: false }`, Quando renderizada, Então aparece `Estado civil: Casado(a)`, `Tabagismo: Não`, `Etilismo: Social` (positiva clínica usa o status preenchido), `Drogas: Não`; e NÃO aparece "Atividade física" (flag não-clínica negativa → omitida — R3), nem strings `tabagismoTem`/`false`.

- **CA9 — Exame físico: sinais vitais agrupados.** Dado `conteudo["exame-fisico"] = { paSistolica: "120", paDiastolica: "80", fc: "72", temperatura: "36.5", spo2: "98" }`, Quando renderizada, Então a linha de sinais vitais é `PA: 120/80 mmHg, FC: 72 bpm, Temp: 36.5 °C, SpO₂: 98%` (só preenchidos, com unidades; `fr`/`glicemia` ausentes não aparecem) e NÃO contém `paSistolica`, `paDiastolica`.

- **CA10 — Exame físico: região anatômica detalhada.** Dado `regioes: [{ caminho: "Tórax", lateralidade: "bilateral", vista: "anterior", texto_exame: "Murmúrio vesicular presente", achados: "", observacoes: "" }]`, Quando renderizada, Então aparece uma linha legível `Tórax (anterior), bilateral: Murmúrio vesicular presente` e NÃO aparecem chaves técnicas `regiao_id`, `timestamp`, `caminho:`, `lateralidade:`.

- **CA11 — Exame físico: lateralidade nula omitida.** Dado uma região com `lateralidade: null`, Quando renderizada, Então a linha não imprime a string `null` nem parênteses/vírgula vazios para lateralidade.

- **CA12 — Exames realizados com tipo e material.** Dado `conteudo["exames-realizados"] = { itens: [{ tipo: "Laboratorial", material: "Sangue", nome: "Hemograma", comentario: "em jejum" }], observacoes: "" }`, Quando renderizada, Então aparece `Hemograma (Laboratorial, Sangue) — em jejum` e NÃO aparecem `tipo:`, `material:`, `comentario:`.

- **CA13 — Procedimentos indicados.** Dado `conteudo["procedimentos-indicados"] = { procedimentos: [{ descricao: "Infiltração", observacao: "joelho direito" }], observacoes: "" }`, Quando renderizada, Então aparece `Infiltração — joelho direito`.

- **CA14 — Fallback genérico humaniza chave não mapeada.** Dado uma seção com `chave` não pertencente às 6 conhecidas e `conteudo[chave] = { atividadeFisicaNivel: "Moderado", algumFlag: false }`, Quando renderizada, Então o rótulo aparece humanizado (ex: `Atividade física nível: Moderado`), o booleano `false` NÃO aparece como `false` nem como `algumFlag:` cru, e nenhuma chave técnica camelCase crua é exibida.

- **CA15 — Seção de texto puro inalterada.** Dado `conteudo[chave]` sendo a string `"Paciente refere dor há 3 dias."`, Quando renderizada, Então o texto é exibido idêntico (trimado), sem rótulos adicionados.

- **CA16 — Paridade modal ↔ PDF.** Dado qualquer das evoluções dos CAs acima, Quando a mesma seção é renderizada na modal e gerada no PDF, Então a string de conteúdo da seção é byte-a-byte idêntica nos dois canais (ambos chamam `formatarSecaoLegivel(s.chave, conteudo[s.chave])`).

- **CA17 — Seção totalmente vazia omitida em ambos.** Dado uma seção cujo `formatarSecaoLegivel` retorna `""`, Quando a modal e o PDF são renderizados, Então a seção não aparece em nenhum dos dois (mesma decisão de exibição derivada do formatador — R7/R9).

- **CA18 — Multi-tenant inalterado.** Dado um usuário do estabelecimento B abrindo uma evolução do estabelecimento A, Quando tenta carregar, Então o comportamento de bloqueio/404 genérico permanece o mesmo de hoje (esta demanda não toca query nem filtro de tenant) e nada é logado com PII.

- **CA19 — Sem regressão de testes.** Dado a suíte existente de `useEvolucaoResumo`/seções, Quando roda após a mudança, Então passa; e o teste `SecaoExameFisico.test.ts` continua verde (esta demanda não toca o componente de edição).

## 8. Riscos e dependências

- **Risco de paridade**: se o PDF ou a modal mantiver qualquer heurística própria de "preenchido"/exibição divergente do formatador, quebra R9. Mitigação: ambos devem decidir exibição pelo resultado de `formatarSecaoLegivel` (string vazia = omite). Atenção ao `preenchido()` local do drawer e ao `.filter` do PDF — alinhar para usar a saída do formatador.
- **Risco de schema variável**: `conteudo` é `unknown`; evoluções antigas podem ter campos faltando ou tipos inesperados. O formatador deve degradar com segurança (campo ausente = omitido), nunca lançar.
- **Dependência de nomes de chave de seção**: os formatadores curados dependem das chaves `hpp`, `h-familiar`, `h-social`, `exame-fisico`, `exames-realizados`, `procedimentos-indicados` (confirmadas em `SecaoProntuario.vue`). Se uma chave de modelo divergir, cai no fallback genérico (degradação aceitável, não quebra).
- **Áreas regressivas a vigiar**: leitura de evolução (modal) e PDF de evolução. Não há impacto em escrita, agenda, financeiro, estoque.
- **`resumoTextual` e `contarSecoesPreenchidas`** (no mesmo arquivo) NÃO devem mudar de comportamento — só `valorSecaoParaTexto` é substituído/renomeado. Se algum outro consumidor importar `valorSecaoParaTexto`, manter compatibilidade ou atualizar todos os call sites (hoje: modal + PDF).

## 9. Observações para execução

**Não-negociável**:
- Ponto único: `formatarSecaoLegivel(chave: string, valor: unknown): string` em `frontend/src/composables/useEvolucaoResumo.ts`. Modal e PDF passam `s.chave`. Não duplicar lógica de formatação em dois lugares (R9).
- Saída é string de texto puro com `\n` entre linhas/itens. Sem markdown, sem `**`, sem HTML embutido (Exame é textarea sem markdown — concatenar puro).
- Campos vazios sempre omitidos; nunca imprimir chave técnica crua nem `true`/`false`.

**Rótulos e formatos canônicos** (o dev usa exatamente estes; ajustes finos de copy podem ser validados pelo QA contra os exemplos dos CAs):
- HPP: `Alergias`, `Medicações de uso`, `Cirurgias`, `Doenças prévias`, `Observações`. Negativas: `Alergias: Nega`, `Medicações de uso: Nega`.
- Item alergia: `{nome} ({observacao})` — observação omitida se vazia.
- Item medicação: junta `{nome}`, `{dose}`, `{frequencia}` separados por espaço/vírgula e `— {motivo}`; `observacoes` ao final entre parênteses se houver. Só campos preenchidos.
- Item cirurgia: `{nome} ({ano})` + ` — {observacao}` se houver.
- Item doença: `{nome} ({observacao})`.
- H-social negativas clínicas: `Tabagismo: Não`, `Etilismo: Não`, `Drogas: Não`. Positivas usam o `*Status`/`*Nivel`/`*Obs` preenchidos.
- Exame físico unidades: PA `mmHg`, FC `bpm`, FR `irpm`, Temp `°C`, SpO₂ `%`, Glicemia `mg/dL`, Peso `kg`, Altura conforme valor (m/cm).
- Região: `{caminho} ({vista}), {lateralidade legível}: {texto_exame || achados}`; vista/lateralidade omitidas quando nulas; `observacoes` da região após travessão se houver. `observacoesExame` geral ao final da seção.

**Liberdade técnica do dev**: organização interna dos formatadores (um objeto-mapa `chave → fn`, helpers privados de "junta campos não-vazios"), nomes de funções auxiliares, e se renomeia `valorSecaoParaTexto` ou mantém um alias. Reuso: criar um helper interno de "linha de campos não-vazios" e reaproveitá-lo entre os formatadores curados (alergia/medicação/cirurgia/exame compartilham o padrão).

**QA**: validar por análise de código + suíte (chrome-devtools indisponível no sandbox — validação visual fica para o usuário em prod). Cobrir os 19 CAs com casos unitários no nível de `formatarSecaoLegivel`, e confirmar que modal e PDF chamam a mesma função com a mesma assinatura.

## 10. Atualização de documentação

**`Docs/DESIGN.md`** — adicionar uma nota curta na seção de padrões de prontuário/leitura: "A renderização legível de seções estruturadas de evolução (drawer 'Ver' e PDF) é centralizada em `formatarSecaoLegivel(chave, valor)` em `useEvolucaoResumo.ts` — ponto único compartilhado por modal e PDF; nunca renderizar JSON cru (`chave: valor`, `true`/`false`) ao usuário; campos vazios e flags de controle são omitidos, negativas clínicas explícitas só para alergias/medicações/tabagismo/etilismo/drogas." Mudança incremental e cirúrgica — não reescrever o documento.

Nenhum outro doc muda (sem arquitetura/infra/LGPD/comandos novos — leitura de dado já existente, sem audit novo, sem schema).
