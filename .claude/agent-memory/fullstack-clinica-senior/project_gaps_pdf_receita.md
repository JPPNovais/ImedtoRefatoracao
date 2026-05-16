---
name: project_gaps_pdf_receita
description: Campos do domínio que o mock de PDF prevê mas o sistema ainda não tem — não inventar, mostrar só o que existe.
metadata:
  type: project
---

Ao gerar PDFs de receita/prontuário/relatório, **não inventar dado de domínio**. Os seguintes campos do mock `PrintPreview` ainda **não existem** no schema atual e devem ser **omitidos** silenciosamente (sem `null`/"—" visível no header):

- `Estabelecimento.Email`, `Estabelecimento.Site`, `Estabelecimento.Tagline`, `Estabelecimento.Cidade`, `Estabelecimento.Cep` — só existem `NomeFantasia`, `RazaoSocial`, `Cnpj`, `Telefone`, `Endereco` (string única), `FotoUrl`.
- `Receita.UsoContinuo` — não existe flag. Não mostrar o título "RECEITA DE USO CONTÍNUO" em nenhum lugar.
- `Paciente.Convenio`, `Paciente.TipoSanguineo` — não existem. Bloco do paciente usa só nome/idade/sexo/CPF/nascimento/telefone.
- `Receita.AssinaturaDigitalStatus` — só hoje vem `NaoAssinada` em emissões reais. Os valores `AssinadaIcp` e `AssinadaMemed` estão no enum mas **não são produzidos pelo fluxo atual**. Selo verde "Assinado digitalmente · ICP-Brasil" só aparece se vier um desses estados — não afirmar ICP por padrão.

**Why:** afirmar dado inexistente é problema regulatório (CFM/ANVISA) e LGPD. Plano `PLANO_REDESIGN_PDF.md` §2.4-2.6 lista esses gaps explicitamente.

**How to apply:** ao tocar header/footer/bloco-paciente em qualquer PDF, confira o aggregate antes — se o campo não existe, omita a linha (não preencha com "—" no header, isso fica feio). Se o produto pedir um desses campos, o caminho é: migration + comando + UI + DTO de leitura, nessa ordem; o PDF já está preparado para receber.
