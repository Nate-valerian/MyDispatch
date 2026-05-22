---
paths:
  - "src/Core/Logistics.Mappings/**/*.cs"
  - "src/Core/Logistics.Application/**/*.cs"
---

# Entity-to-DTO Mapping (Mapperly)

- All mapping via Riok.Mapperly mappers in `src/Core/Logistics.Mappings/` — no manual mapping in handlers
- Extension method pattern: `entity.ToDto()`
- `[MapperIgnoreSource]` for navigation properties, internal fields
- `[MapperIgnoreTarget]` for computed fields set manually after mapping

```csharp
[Mapper]
public static partial class EntityMapper
{
    [MapperIgnoreSource(nameof(Entity.NavProp))]
    public static partial EntityDto ToDto(this Entity entity);
}

// Computed fields: map then override with `with` expression
public static EntityDto ToDto(this Entity entity, int computed)
{
    var dto = entity.ToDto();
    return dto with { ComputedField = computed };
}
```
