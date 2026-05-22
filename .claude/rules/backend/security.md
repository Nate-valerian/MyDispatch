---
paths:
  - "**/*.cs"
---

# Security Rules

- **Input validation**: FluentValidation for all user inputs, parameterized queries (EF Core default)
- **Auth**: Every controller action needs `[Authorize]`, use policy-based authorization with `Permission` constants
- **Tenant isolation**: Validate tenant context for all tenant-scoped operations
- **Secrets**: Never hardcode — use appsettings/env vars/Key Vault. Never log passwords, tokens, PII
- **Files**: Validate extension allowlist, check MIME type matches, `[RequestSizeLimit]`, store in Azure Blob
- **SQL**: Never `FromSqlRaw` with string concatenation — use `FromSqlInterpolated`
- **Webhooks**: Validate signatures (Stripe, ELD), constant-time comparison, audit logging
