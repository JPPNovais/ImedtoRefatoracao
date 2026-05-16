---
name: qa-posdeploy-5ed930c-2026-05-13
description: QA do deploy `5ed930c` (5 P1 prometidas). 1 OK total, 2 quebradas, 2 parciais. Encoding e migração frontend-PII são padrões a checar.
metadata:
  type: project
---

# QA pós-deploy 5ed930c — 2026-05-13

Validação das 5 correções P1 anunciadas. Veredito: 🟡 não regredir, mas hotfix urgente.

## Status real entregue (vs. anunciado)

| P1 anunciada | Real |
|----|----|
| #1 Modal cancelar | ❌ ainda usa `window.prompt` no card da agenda |
| #2 Trocar senha | ✅ entregue 100% (modal, 4 validações, sessão encerra) |
| #3 UI LGPD | ⚠️ tela existe e funciona, mas todo o copy tá sem acentos ("Protecao", "voce", "irreversivel", "anonimizacao") |
| #4 Anti-enum login | ⚠️ mensagem unificada OK; link "Reenviar e-mail" não aparece nunca |
| #5 Minimização PII autocomplete | ❌ backend novo (`/api/paciente/busca-rapida` retorna `[{id, nomeCompleto}]`) **existe e funciona**, mas frontend ignora — continua chamando `/api/paciente?tamanho=200` no boot da agenda e mostrando telefone no autocomplete |

## Aprendizados a aplicar nas próximas sessões

**1. Toda P1 backend deve ser validada com chamada direta e com uso real do frontend, em duas etapas.**
**Why:** P1 #5 foi marcada como "feita" pelo time mas só o backend foi entregue — o frontend continua usando o endpoint antigo e trafegando PII. Validar só o endpoint via fetch teria passado falso positivo.
**How to apply:** para correções que prometem mudar payload/endpoint, sempre rodar 2 testes: (a) `fetch()` direto pelo DevTools pra ver se o novo endpoint responde como spec; (b) navegar pela UI real com Network aberto pra ver qual endpoint o front efetivamente chama.

**2. Strings hardcoded em pt-BR são fonte recorrente de bugs de encoding.**
**Why:** Tela `/minha-conta/lgpd` tem ~10 ocorrências de palavras sem acentos no copy. Provavelmente strings coladas via terminal/clipboard com locale não-UTF8.
**How to apply:** em toda tela nova ou alterada, fazer um pass de busca pelas palavras-chave `protecao|acao|nao|voce|sao|tambem|historico|prontuario|informacoes` para detectar encoding ruim. Se for ver código antes da próxima sessão, sugerir mover copies pro arquivo de i18n.

**3. "Anti-enumeração funcional" ≠ "UX completa". Mensagem genérica precisa vir junto com via de auto-serviço.**
**Why:** P1 #4 entregou anti-enum (bom!) mas removeu o link "Reenviar confirmação" do fluxo, deixando usuário com conta pendente sem saída. O link deve ser **sempre** visível após erro de login (não condicional ao tipo de erro).
**How to apply:** quando fluxo de login retorna erro genérico por razão de segurança, garantir que os 2 caminhos de auto-serviço (esqueci senha + reenviar confirmação) estejam acessíveis sem depender da identificação do erro específico.

**4. `window.prompt` é cheiro de "feature feita pela metade" — sempre buscar antes de declarar pronto.**
**Why:** P1 #1 supostamente substituiu o `prompt` por modal, mas o `prompt` continua vivo no botão Cancelar do card da agenda. A correção provavelmente foi feita em outro componente.
**How to apply:** em PR que substitui modal nativo, rodar `grep -r "window.prompt\|window.confirm" frontend/src/` antes de marcar como pronto. Múltiplos pontos de chamada são comuns em UI legada.

## Arquivos com bugs confirmados (pra próxima sessão verificar)

- Frontend: views/components da agenda (card de agendamento) ainda chamam `window.prompt`.
- Frontend: store/service da agenda chama `pacientesService.listar({tamanho:200})` no boot; precisa migrar para chamada server-side dentro do modal Novo Agendamento.
- Frontend: `/minha-conta/lgpd` (provável `LgpdView.vue`) — copies hardcoded sem acentos.
- Frontend: `/login` — link "Reenviar e-mail de confirmação" condicional onde deveria ser incondicional.
- Frontend: modal de trocar senha — não reseta `error` ao mudar campos.

## Backend OK (não tocar)

- `GET /api/paciente/busca-rapida?nome=…&limite=8` retorna `[{id, nomeCompleto}]` corretamente.
- `POST /api/auth/login` retorna 401 unificado pra credenciais inválidas e conta inexistente.
- `POST /api/minha-conta/trocar-senha` valida senha atual, exige ≥8 chars, exige diferente da atual, encerra sessões.
- `GET /api/minha-conta/exportar-dados` retorna JSON com perfil + vinculos + notificacoes + consentimentos.

Relaciona-se com [[project_qa_rodada3_2026_05_13]] (rodada anterior).
