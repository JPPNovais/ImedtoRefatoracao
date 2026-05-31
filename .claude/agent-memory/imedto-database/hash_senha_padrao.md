---
name: hash-senha-padrao
description: Algoritmo BCrypt+pepper HMAC-SHA256 usado em BcryptPasswordHasher — como regenerar hash para seeds
metadata:
  type: project
---

Algoritmo em `BcryptPasswordHasher.cs`:
1. `pepper_bytes = Convert.FromBase64String(pepper)`
2. `peppered_bytes = HMAC-SHA256(key=pepper_bytes, data=senha_bytes)`
3. `peppered_b64 = Convert.ToBase64String(peppered_bytes)`
4. `hash = BCrypt.HashPassword(peppered_b64, workFactor=12)`

Para regenerar hash de seed, rodar projeto HashGeneratorApp em /tmp/HashGeneratorApp com a dependência `BCrypt.Net-Next`.

**Hash de "123123" com pepper de dev** (`tRFYrxgSccCs3X6IMUrTReJNRNN1ToLQymktIb+vHiQ=`):
`$2a$12$2NM7d16QKx9TDGy4Z8NVCuCjqUG/CZIs0pox9lioJRuwCajzU.nyK`

**Why:** Se o pepper mudar (rotação SSM) o hash muda. Nunca reusar hash sem confirmar o pepper vigente.

**How to apply:** Usar este hash apenas em seed Development. Em prod, hash gerado pelo CLI seed-admin com pepper lido do SSM em runtime.
