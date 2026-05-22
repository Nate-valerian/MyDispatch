---
paths:
  - "src/Client/Logistics.Angular/**/*.ts"
  - "src/Client/Logistics.Angular/**/*.html"
---

# Angular Security

- **XSS**: Never `[innerHTML]` with user content. Use `DomSanitizer` if dynamic HTML needed. Avoid `bypassSecurityTrustHtml()`.
- **Auth**: All API calls include auth headers (interceptor). Route guards for protected routes. Check permissions before showing sensitive UI.
- **Data**: No sensitive data in localStorage/sessionStorage. HttpOnly cookies for tokens. Clear on logout.
- **Forms**: Validate on client AND server. Reactive forms with validators. Sanitize file uploads.
