---
name: project-assinatura-digital-concluida
description: Feature assinatura digital ICP-Brasil implementada (briefing 2026-06-01_001) — backend + frontend + testes verdes.
metadata:
  type: project
---

Briefing 2026-06-01_001 implementado. Backend 0 erros/warnings, 1169 testes verdes. Frontend build verde.

**Why:** Médicos precisam assinar receitas com validade jurídica ICP-Brasil (CFM Res. 2.299/2021). MVP usa BirdID (Soluti).

**How to apply:** Feature aguarda liberação do canal comercial BirdID pela Soluti. O stub `BirdIdAssinaturaProvider` em Infrastructure/AssinaturaDigital/ loga + retorna homologação. A validação HMAC do webhook é funcional. Quando canal for liberado, substituir apenas o corpo de `DispararAssinaturaAsync` em `BirdIdAssinaturaProvider.cs` sem alterar handlers.

Arquivos principais criados:
- Domain: `IAssinaturaDigitalProvider.cs`, `IAssinaturaCertificadoRepository.cs`, `IAssinaturaAuditLogRepository.cs` em `Domain/AssinaturaDigital/`
- Application: 5 command handlers + 2 query handlers em `Application/AssinaturaDigital/`
- Infrastructure: `BirdIdAssinaturaProvider.cs`, `AssinaturaCertificadoRepository.cs`, `AssinaturaDigitalQueryRepository.cs`, `ExpirarAssinaturasPendentesJob.cs`
- API: `ReceitaAssinaturaController.cs`, `MedicoCertificadoController.cs`
- Job: registrado em `JobsRegistrados.cs` como `expirar-assinaturas-pendentes` (1×/hora)
- Frontend: `assinaturaDigitalService.ts`, `assinaturaDigitalStore.ts`, `AssinaturaStatusBadge.vue`, `AssinaturaOnboardingModal.vue`, `AssinaturaPollingIndicator.vue`
- Integração: `ReceitasPacienteTab.vue` estendido (não reescrito) com ações de assinatura

Nota importante: `ExpirarAssinaturasPendentesCommandHandler` (Application) existe mas o job usa diretamente os repos do Domain (Infrastructure→Application não é permitido). O handler pode ser chamado via bus se necessário.
