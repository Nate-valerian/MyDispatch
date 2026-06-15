# DispatchLoad / MyDispatch Case Study

DispatchLoad is an AI-powered logistics SaaS platform for trucking companies. It brings dispatch, fleet management, customer tracking, driver workflows, invoicing, payroll, compliance, integrations, and AI-assisted decision making into one multi-tenant system.

## Product Scope

The platform is built for freight, vehicle transport, and intermodal drayage operations. It supports multiple user groups:

- **Dispatchers** manage loads, trips, drivers, trucks, load boards, and AI dispatch sessions.
- **Drivers** receive assignments, view trip details, capture documents, and communicate from the mobile app.
- **Customers** track shipments, access documents, and pay invoices through a customer portal.
- **Owners and managers** review reports, payroll, expenses, safety data, and operational performance.
- **Admins** manage tenants, plans, features, subscriptions, and platform-level settings.

## Technical Highlights

| Area | Implementation |
| --- | --- |
| Backend | .NET 10, ASP.NET Core, Clean Architecture, DDD, CQRS, MediatR |
| Frontend | Angular 21, TypeScript, PrimeNG, Tailwind CSS, NgRx Signals |
| Mobile | Kotlin Multiplatform, Compose Multiplatform, Ktor, generated OpenAPI clients |
| Database | PostgreSQL, EF Core, tenant-isolated databases |
| Identity | Duende IdentityServer, JWT, role-based access, API keys |
| Real time | SignalR messaging, tracking, notifications |
| Background jobs | Hangfire jobs for sync, reminders, payroll, retention, and AI dispatch sessions |
| Payments | Stripe payments, Stripe Connect, subscriptions, payroll payouts, webhooks |
| Routing | Mapbox geocoding, route planning, trip optimization |
| Documents | POD, BOL, invoices, payroll PDFs, blob storage |
| AI | Dispatch agent, LLM provider abstraction, tool registry, quotas, pricing, MCP server |
| Observability | .NET Aspire, Docker, OpenTelemetry |

## Architecture

DispatchLoad uses a modular clean architecture:

- `Domain` owns entities, value objects, enums, and domain rules.
- `Application` owns commands, queries, validators, workflow services, and feature behavior.
- `Application.Abstractions` defines infrastructure ports and contracts.
- `Infrastructure` projects implement persistence, AI, routing, payments, communications, documents, storage, ELD, load boards, tax, and VIN decoding.
- `Presentation` projects expose the API, Identity Server, MCP server, database migrator, and Telegram bot.
- `Client` projects contain Angular portals, the marketing website, demo video tooling, and Kotlin driver app.

The system is multi-tenant. A master database stores tenants, plans, subscriptions, and platform state. Each tenant receives its own isolated tenant database. Tenant resolution happens per request from MCP API keys, tenant headers, or JWT claims.

## AI Dispatch

The AI dispatch agent is designed for logistics workflows where decisions need to be explainable. It can:

- Match loads to eligible trucks and drivers.
- Check dispatch eligibility, HOS constraints, license status, hazmat/ADR equipment, and operational constraints.
- Plan trips and route assignments.
- Search load boards and evaluate opportunities.
- Run in human-in-the-loop or autonomous mode.
- Record decisions, reasoning, tool calls, and approval/rejection history.

The same tool registry powers both the internal AI dispatch agent and the MCP server, so external AI tools can access controlled fleet operations through the same command/query surface.

## Integrations

DispatchLoad includes realistic integration boundaries:

- Stripe and Stripe Connect for payments, subscriptions, and payouts
- Mapbox for geocoding, routing, and route optimization
- Firebase for push notifications
- ELD providers such as Samsara, Motive, Geotab, TT ELD, and demo providers
- Load boards such as DAT, Truckstop, 123Loadboard, and demo providers
- Blob storage providers including Azure Blob Storage, Cloudflare R2, and local file storage
- NHTSA VIN decoding

## What This Demonstrates

This project demonstrates the ability to build beyond a single CRUD app:

- Designing a multi-tenant SaaS product with multiple user roles and portals
- Modeling a complex business domain with operations, finance, compliance, identity, and integrations
- Building full-stack workflows from database to API to Angular UI to mobile
- Handling real-time communication, background jobs, webhooks, generated clients, and external APIs
- Applying production architecture patterns while keeping features discoverable and testable
- Designing AI agent workflows with guardrails, quotas, human review, and explainable decisions

## Screenshots

Screenshots are available in the main [README](README.md) and [screenshots guide](docs/screenshots.md).

## Local Development

The recommended local entry point is .NET Aspire:

```bash
dotnet run --project src/Aspire/Logistics.Aspire.AppHost
```

See the [local development guide](docs/getting-started/local-development.md) for full setup notes.
