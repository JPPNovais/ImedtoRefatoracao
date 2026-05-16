---
name: elementfrompoint-armadilha-reka-dialog
description: Não validar "toast acima de overlay Reka" com elementFromPoint; usar screenshot + DOM props.
metadata:
  type: feedback
---

Ao validar se um toast renderizado via `<Teleport to="body">` está visível acima de um overlay de AppDrawer/AppDialog (Reka UI), **NÃO** usar `document.elementFromPoint(cx, cy)` como critério de "está em cima".

**Why:** Reka UI, ao abrir um modal, aplica `pointer-events: none` no `<body>`. O toast (filho direto de body via Teleport) herda `pointer-events: none`. Hit testing (`elementFromPoint`) ignora elementos com `pointer-events: none` e retorna o que está abaixo — neste caso o overlay (que explicitamente seta `pointer-events: auto`). Isso induz a um falso-positivo de "toast atrás do overlay" mesmo quando ele está visivelmente acima na renderização. No QA loop 3 do commit `2d659b1`, isso quase me fez devolver o fix como regressivo — só salvou o screenshot.

**How to apply:** A validação correta de "toast acima do overlay" combina (1) screenshot mostrando o toast visualmente sobre o overlay escuro; (2) propriedades DOM: `parentElement === document.body`, `position: fixed`, `z-index >= 50`, `opacity > 0`, `getBoundingClientRect()` dentro da viewport. Aplicável a qualquer toast/snackbar/popover que use Teleport com Reka/Radix/Headless UI dialogs abertos.
