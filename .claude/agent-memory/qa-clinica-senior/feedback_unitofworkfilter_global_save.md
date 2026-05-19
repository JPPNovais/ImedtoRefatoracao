---
name: unitofworkfilter-global-save
description: SaveChangesAsync é automático em todas as actions via UnitOfWorkFilter global — não cobrar SaveChanges no Salvar do repositório.
metadata:
  type: feedback
---

`UnitOfWorkFilter` é registrado **globalmente** em [Program.cs:166](backend/src/Services/Imedto.Backend.API/Program.cs#L166):
```csharp
options.Filters.Add<UnitOfWorkFilter>();
```

Ele abre `_uofwFactory.Begin()` antes da action e chama `scope.CommitAsync()` se não houve exceção — o commit faz `SaveChangesAsync()` no `AppDbContext` scoped da request. Resultado: **toda action commita o DbContext automaticamente**, sem precisar de `[UnitOfWork]` no controller nem `SaveChangesAsync` no `Salvar` do repositório.

**Why:** Erro de revisão recorrente — ao olhar `ProfissionalRepository.Salvar` ou `EstabelecimentoRepository.Salvar` no caminho de update (linhas `_context.X.Update(entity)` sem SaveChanges), parece bug. Mas funciona porque o filter global faz o commit. Verificado em prod no QA 2026-05-19 da feature foto profissional (PUT/DELETE retornaram 200/204 corretos, persistência confirmada).

**How to apply:** Ao revisar um handler/repositório que muda aggregate, **não** marcar como bug a ausência de `SaveChanges` se a operação acontece numa action MVC normal. Marcar como bug só se:
- For uma rota background/scheduled job que não passa pelo filter, OU
- For uma action com `[ServiceFilter]` que substitui o filter global, OU
- O fluxo precisar de múltiplos commits dentro da mesma request (ex: side-effects que dependem de Id auto-gerado — nesse caso o `Salvar` precisa de SaveChanges intermediário, como já faz o `EstabelecimentoRepository.Salvar` no INSERT linha 43).

**Caveat:** Excecoes lançadas APÓS o último await do handler ainda fazem rollback porque o `EfUnitOfWorkScope` só comita se o `result.Exception` está null. Logo, lançar `BusinessException` após mudar entidade = rollback automático. Bom comportamento.
