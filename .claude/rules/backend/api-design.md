---
paths:
  - "src/Presentation/Logistics.API/**/*.cs"
---

# API Design Standards

## REST Endpoints
- Plural lowercase nouns: `/loads`, `/customers`, `/trucks`
- No hyphens, no camelCase/PascalCase in URLs
- Path params for IDs: `/loads/{id}`, nested: `/loads/{id}/trips`
- Custom actions as sub-resources: `POST /loads/import`

## Controller Structure
- Route: `[Route("resources")]`, `[Produces("application/json")]`
- Primary constructor for DI, `[ProducesResponseType]` for all responses
- Auth: `[Authorize(Policy = Permission.{Entity}.View|Manage)]`
- Errors: `ErrorResponse.FromResult(result)`, appropriate status codes, no internal details

## HTTP Methods

| Action | Method | Route | Returns |
|--------|--------|-------|---------|
| List | GET | `/resources` | `PagedResponse<Dto>` |
| Get | GET | `/resources/{id}` | `Dto` or 404 |
| Create | POST | `/resources` | `Dto` or 204 |
| Update | PUT | `/resources/{id}` | 204 or 400 |
| Delete | DELETE | `/resources/{id}` | 204 or 404 |
