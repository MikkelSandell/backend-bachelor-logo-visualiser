# GitHub Copilot Instructions — LogoVisualizer Backend

## Project overview

**LogoVisualizer** is an ASP.NET Core 10 Web API backend for a promotional merchandise company. It serves two clients:
- A **Logo Viewer** (embeddable, public) — lets end-customers upload a logo and preview it on a product.
- An **Admin Tool** (authenticated) — lets internal staff configure products and define rectangular print zones.

This is a **bachelor's thesis** project. Code and docs are in English; UI strings are in Danish.

**Current data source**: SQL Server 2022 via Docker (`docker compose up -d`). EF Core migrations are applied automatically on startup. `MidoceanSeederService` seeds all products from `Midocean-print-data.json` on first run (idempotent). The backend falls back to the JSON file if the DB is unreachable. `ProductDataService` handles this DB-first / JSON-fallback logic. Zone CRUD (`PrintZonesController`) writes directly to the DB.

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
- **ORM**: Entity Framework Core 10 — active with SQL Server 2022 (Docker)
- **Auth**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) — dev token via `POST /api/auth/dev-token` in Development; production tokens from external *Master* app
- **Rate limiting**: `AspNetCoreRateLimit`
- **Image processing**: `SixLabors.ImageSharp`
- **API docs**: Swashbuckle (OpenAPI 3 / Swagger) — available at `http://localhost:5000/swagger`

---

## Data model summary

### EF Core entities (active — SQL Server via Docker)
```
Product (Id, Title, ImagePath, ImageWidth, ImageHeight)
  └── PrintZone[] (Id, ProductId, Name, X, Y, Width, Height,
                   MaxPhysicalWidthMm, MaxPhysicalHeightMm, MaxColors, ImageUrl)
        └── PrintZoneTechnique[] → PrintTechnique (Id, Name)
```

### Adapted DTOs (active, from Midocean JSON)
```
AdaptedProductDto (Id, Title, ImageUrl, ImageWidth, ImageHeight)
  └── AdaptedPrintZoneDto[] (Id, Name, X, Y, Width, Height,
                              MaxPhysicalWidthMm, MaxPhysicalHeightMm,
                              AllowedTechniques: string[], ImageUrl)
```
`ImageUrl` per zone is the blank position photo pinned to `item_color_numbers[0]` so all zones show the same colour.
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
