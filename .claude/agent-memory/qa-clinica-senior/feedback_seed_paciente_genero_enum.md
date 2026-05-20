---
name: seed-paciente-genero-enum
description: Seed SQL direto de paciente exige genero=nome do enum string (NaoInformado/Masculino/Feminino/Outro), não sigla.
metadata:
  type: feedback
---

Ao inserir paciente direto no RDS via `INSERT INTO pacientes`, o campo `genero` é mapeado pelo EF como `enum GeneroPaciente` em modo string (`HasConversion<string>()`). Valores válidos: `NaoInformado`, `Masculino`, `Feminino`, `Outro`. Inserir `'F'` ou `'M'` (siglas) causa **500** em qualquer endpoint que carregue o paciente via EF (e.g., `GET /api/pacientes/{id}/termos` cai antes do handler com `InvalidOperationException: Cannot convert string value 'F' to GeneroPaciente`).

**Why:** Não bater em 500 no QA por seed errado — gera falso P0 e gasta ciclo procurando bug onde não há.

**How to apply:** Em qualquer `INSERT INTO pacientes` em túnel SQL pra setup de QA, usar os literais do enum completos. Idem para outros enums string no projeto (status de termo, assinaturaTipo, etc.) — verificar valores válidos em `Domain/*/Enum*.cs` antes de inserir.
