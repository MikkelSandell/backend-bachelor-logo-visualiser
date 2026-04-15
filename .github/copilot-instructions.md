# GitHub Copilot Instructions — LogoVisualizer Backend

## Project overview

**LogoVisualizer** is an ASP.NET Core 8 Web API backend for a promotional merchandise company. It serves two clients:
- A **Logo Viewer** (embeddable, public) — lets end-customers upload a logo and preview it on a product.
- An **Admin Tool** (authenticated) — lets internal staff configure products and define rectangular print zones.

This is a **bachelor's thesis** project. Code and docs are in English; UI strings are in Danish.

---

## Solution layout

```
LogoVisualizer.sln
├── LogoVisualizer.Api      — Web API (controllers, DTOs, services, Program.cs)
│   ├── Controllers/        — HTTP controllers
│   ├── Data/               — Static data files (midocean-top10.json)
│   ├── DTOs/               — Request/response record types
│   ├── Extensions/         — IApplicationBuilder helpers
│   ├── Helpers/            — Utility/helper classes
│   └── Services/           — Service interfaces + implementations
└── LogoVisualizer.Data     — EF Core models, AppDbContext, repositories
```

---

## Tech stack

- **Runtime**: .NET 8, ASP.NET Core (controller-based, not Minimal API)
- **ORM**: Entity Framework Core 8 with SQL Server
- **Auth**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) — tokens from external *Master* app
- **Rate limiting**: `AspNetCoreRateLimit`
- **Image processing**: `SixLabors.ImageSharp`
- **API docs**: Swashbuckle (OpenAPI 3 / Swagger)

---

## Data model summary

```
Product (Id, Title, ImagePath, ImageWidth, ImageHeight)
  └── PrintZone[] (Id, ProductId, Name, X, Y, Width, Height,
                   MaxPhysicalWidthMm, MaxPhysicalHeightMm, MaxColors)
        └── PrintZoneTechnique[] → PrintTechnique (Id, Name)
```

---

## Conventions to follow

- **Namespaces**: `LogoVisualizer.Api` for API project, `LogoVisualizer.Data` for data project.
- **DTOs**: use `record` types. Response DTOs have a `static FromEntity()` factory method.
- **Repositories**: inject `IProductRepository` / `IPrintZoneRepository` — do not inject `AppDbContext` in controllers unless a direct EF query is unavoidable.
- **Services**: business logic that does not belong in a controller goes in `LogoVisualizer.Api/Services/`. Define an interface (`IXxxService`) and register as `AddSingleton` or `AddScoped` as appropriate in `Program.cs`.
- **Async**: every controller action and repository method accepts `CancellationToken ct`.
- **Return types**: `NotFound()`, `BadRequest(new { error = "..." })`, `ValidationProblem()`, `NoContent()`, `CreatedAtAction()` — never throw HTTP-related exceptions.
- **Auth**: `[Authorize]` on all write endpoints (POST/PUT/DELETE). No attribute on GET / public endpoints.
- **File upload security**: always save with a GUID-based filename — never the user-supplied filename.

---

## API endpoint conventions

- Admin write routes: `[Authorize]` + `[RequestSizeLimit(...)]`
- Public routes: no auth, rate-limited via `IpRateLimiting` config
- Print zone routes are nested: `api/products/{productId}/zones`

---

## Midocean sample data

`LogoVisualizer.Api/Data/midocean-top10.json` contains 10 real Midocean supplier products with full print-position data (positions, techniques, images, coordinate points). It is loaded once at startup by `MidoceanProductService` (singleton) and exposed via:

- `GET /api/midocean-products` — all 10 products
- `GET /api/midocean-products/{masterCode}` — single product by `master_code` (e.g. `S11500`)

These endpoints are public (no `[Authorize]`). Do not modify the JSON file directly — re-extract from `Midocean-print-data.json` in the project root if the data needs updating.

---

## What NOT to add

- 3D rendering or AR
- PMS colour matching
- Production file generation (CMYK, embroidery stitches)
- Shopify / third-party e-commerce integrations
- Automatic logo vectorisation
