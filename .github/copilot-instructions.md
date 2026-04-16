# GitHub Copilot Instructions — LogoVisualizer Backend

## Project overview

**LogoVisualizer** is an ASP.NET Core 10 Web API backend for a promotional merchandise company. It serves two clients:
- A **Logo Viewer** (embeddable, public) — lets end-customers upload a logo and preview it on a product.
- An **Admin Tool** (authenticated) — lets internal staff configure products and define rectangular print zones.

This is a **bachelor's thesis** project. Code and docs are in English; UI strings are in Danish.

**Current data source**: No database is in use. All product data is served from `LogoVisualizer.Api/Data/midocean-top10.json` via `MidoceanProductService`. The DB-backed endpoints exist in code but require a database connection. `app.ApplyMigrations()` is commented out in `Program.cs`.

---

## Solution layout

```
LogoVisualizer.sln
├── LogoVisualizer.Api      — Web API (controllers, DTOs, services, Program.cs)
│   ├── Controllers/        — HTTP controllers (MidoceanProductsController is the active one)
│   ├── Data/               — Static data files (midocean-top10.json)
│   ├── DTOs/               — Request/response record types (incl. AdaptedProductDto)
│   ├── Extensions/         — IApplicationBuilder helpers
│   ├── Helpers/            — Utility/helper classes
│   ├── Properties/         — launchSettings.json (forces Development environment)
│   └── Services/           — Service interfaces + implementations
└── LogoVisualizer.Data     — EF Core models, AppDbContext, repositories
    └── Migrations/         — Created, not yet applied (no DB)
```

---

## Tech stack

- **Runtime**: .NET 10, ASP.NET Core (controller-based, not Minimal API)
- **ORM**: Entity Framework Core 10 — wired but not active (no DB)
- **Auth**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) — tokens from external *Master* app (wired, not active)
- **Rate limiting**: `AspNetCoreRateLimit`
- **Image processing**: `SixLabors.ImageSharp`
- **API docs**: Swashbuckle (OpenAPI 3 / Swagger) — available at `http://localhost:5000/swagger`

---

## Data model summary

### EF Core entities (DB-backed, not active)
```
Product (Id, Title, ImagePath, ImageWidth, ImageHeight)
  └── PrintZone[] (Id, ProductId, Name, X, Y, Width, Height,
                   MaxPhysicalWidthMm, MaxPhysicalHeightMm, MaxColors)
        └── PrintZoneTechnique[] → PrintTechnique (Id, Name)
```

### Adapted DTOs (active, from Midocean JSON)
```
AdaptedProductDto (Id, Title, ImageUrl, ImageWidth, ImageHeight)
  └── AdaptedPrintZoneDto[] (Id, Name, X, Y, Width, Height,
                              MaxPhysicalWidthMm, MaxPhysicalHeightMm,
                              AllowedTechniques: string[])
```

Technique code mapping: `TR/ST1/SP` → `screen_print`, `E/EM` → `embroidery`,
`EN/B` → `engraving`, `SL/SA` → `sublimation`, `DTG/TDT/TT` → `digital_print`,
`TP/P` → `pad_print`. Unknown codes are silently dropped.

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

## Midocean data — active endpoints

`LogoVisualizer.Api/Data/midocean-top10.json` contains 10 Midocean supplier products with full print-position data. Loaded once at startup by `MidoceanProductService` (singleton) and exposed via:

- `GET /api/midocean-products` — all 10 products (raw Midocean format)
- `GET /api/midocean-products/{masterCode}` — single product (raw)
- `GET /api/midocean-products/as-products` — all 10 adapted to `AdaptedProductDto` (frontend shape)
- `GET /api/midocean-products/{masterCode}/as-product` — single adapted product

These are the primary endpoints used by the frontend. Do not modify the JSON file directly — re-extract from `Midocean-print-data.json` in the project root if data needs updating.

---

## What NOT to add

- 3D rendering or AR
- PMS colour matching
- Production file generation (CMYK, embroidery stitches)
- Shopify / third-party e-commerce integrations
- Automatic logo vectorisation
