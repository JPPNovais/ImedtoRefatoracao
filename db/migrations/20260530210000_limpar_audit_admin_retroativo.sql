-- Cleanup retroativo Wave 7 — remove 78% do volume sem valor forense.
-- Ações cortadas: LOGIN_OK (65%), LOGOUT (5%), ABRIR_DETALHE_TENANT (8%).
-- Dev complementa com job diário (LimparAuditAdminJob) e corte no código (Frente 1).
--
-- Idempotente: segunda execução deleta 0 linhas e retorna sucesso.
-- NÃO toca: LOGIN_FAIL, REVELAR_CPF_DONO e todas as mutações (retenção legítima 1-2 anos).
-- Volume estimado no momento da geração: 158 linhas (131 LOGIN_OK + 16 ABRIR_DETALHE_TENANT + 11 LOGOUT).

DELETE FROM imedto_admin_audit_log
WHERE acao IN ('LOGIN_OK', 'LOGOUT', 'ABRIR_DETALHE_TENANT');
