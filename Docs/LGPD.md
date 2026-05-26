# LGPD e segurança de dados sensíveis

> **Quando ler**: ao tocar feature que envolve paciente, prontuário, agendamento, financeiro, anexo, audit, dado pessoal de usuário, mensagem de erro, log estruturado, payload exposto ao cliente.
>
> **Quando atualizar**: ao introduzir novo tipo de dado pessoal, novo endpoint que expõe PII, nova regra de retenção/exclusão, novo audit. **Responsabilidade primária: `imedto-business-analyst`** (LGPD é premissa de produto, não apenas técnica).

---

Sistema de saúde → dados pessoais sensíveis (Art. 5º II e Art. 11 LGPD). LGPD é **premissa de design**, não checklist de fim de PR — a cada feature nova pergunte: *"este dado é necessário? Está minimizado? Tem RLS? Há audit trail? Pode vazar em log/erro?"*.

## Requisitos obrigatórios

- **Minimização**: query/DTO retorna apenas os campos que a tela usa. Não trazer `cpf`, `data_nascimento`, `telefone`, etc. se a tela não exibe.
- **Direitos do titular**: endpoints de export (`GET /api/minha-conta/exportar-dados`) e exclusão (`DELETE /api/minha-conta`).
- **Audit trail**: log de acesso a dados de paciente/prontuário em audit table (quem, quando, qual registro).
- **Não vazar PII**:
  - Nunca incluir CPF, telefone, e-mail ou nome completo em log estruturado ou em mensagem de erro retornada ao cliente.
  - Nunca retornar token, hash de senha ou ID interno de auth em payload.
  - Mensagens de erro de validação devem ser genéricas ("paciente não encontrado") em vez de descrever o dado consultado.
- **Sem RLS** no Postgres (decisão arquitetural — ver [INFRA.md §Decisões](INFRA.md#decisões-já-tomadas-não-reabrir-sem-motivo-forte)). Defense-in-depth fica no backend (filter multi-tenant + `[RequiresPapel]` + mensagens genéricas).
- **Filtro por estabelecimento**: queries de domínio sempre exigem `estabelecimento_id` no `WHERE` ou no `Include`/join. Multi-tenant é regra, não exceção.
- **Consentimento**: se a feature coleta dado novo do titular (ex: novo campo de paciente), avaliar se precisa de aviso/consentimento explícito antes de armazenar.

## Regras de segurança e performance

- As regras do front que dependem de retorno ou input da API precisam estar sendo validadas também no backend, para evitar de pessoas tentando passar informação direto pela API. Defense-in-depth: front é UX, **back é a fonte da verdade**.
- As APIs precisam respeitar segurança de autenticação — não pode deixar que uma pessoa altere registros caso ela não tenha a devida permissão para isso.
- Todo o site precisa ser pensado de forma que ele pode escalar, ter muitas requisições ao mesmo tempo. Precisa ter resiliência e pensar em performance — não pode demorar o retorno, mas precisa estar muito bem otimizado para não pesar para o usuário.
- Deve buscar basicamente as informações necessárias para aquele momento — caso não tenha clicado em uma aba que ainda não vai utilizar, não precisa ficar fazendo consulta desnecessária (busca apenas quando precisar de fato).
- As rotas que puderem ser reutilizadas precisam ser feitas, para evitar criação de novos endpoints desnecessários que fazem praticamente a mesma coisa.
- O código precisa ser simples de entendimento e componentizado para reutilizar o que for possível em outras partes evitando códigos duplicados.
- Páginas do site precisam estar centralizadas no meio, evitando deixar espaço em branco só na direita ou só na esquerda (ver [DESIGN.md §Container](DESIGN.md#experiência-consistente-em-todo-o-site)).
- Os componentes do front precisam estar padronizados e reutilizados como um todo de acordo com sua necessidade — design system primeiro.
- As regras de negócio devem estar todas no backend, transparente para o front, a fim de evitar problemas de segurança também.

## Checklist multi-tenant — premissa não-negociável

Antes de cada commit que toca dados de domínio (paciente, agendamento, prontuário, financeiro, equipe, estoque, orçamento), valide:

1. **Filtro por `estabelecimento_id`** em todo `WHERE`/join de domínio.
2. **Verificação de vínculo** do usuário com o estabelecimento (papel + escopo do vínculo).
3. **Mensagem genérica em erro** ("não encontrado") — nunca revelar se o registro existe em outro tenant.
4. **Repositório falha-fechada**: ausência de tenant claim → retorna vazio/throws, nunca query global.
