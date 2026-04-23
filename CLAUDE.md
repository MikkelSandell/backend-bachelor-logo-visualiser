# CLAUDE.md — LogoVisualizer Backend

This file gives Claude (and other AI coding assistants) the context needed to work effectively in this codebase without repeated explanation.

---

## What this project is

**LogoVisualizer** is an internal .NET 10 backend service for a promotional merchandise company. It allows:
- **End-customers** to upload their logo and preview it on a product (t-shirts, mugs, pens, etc.) within defined print zones.
- **Admin users** to set up products by uploading product images and drawing rectangular print zones, each with physical size limits and allowed print techniques.

The project is a **bachelor's thesis** deliverable. Code and documentation are in English; UI labels are in Danish.

---

## Tech Stack

| Layer              | Technology                                        |
|--------------------|---------------------------------------------------|
| Web API framework  | ASP.NET Core 10 (controller-based)                |
| ORM                | Entity Framework Core 10                          |
| Database           | SQL Server 2022 — running in Docker (`logo-db` container) |
| Authentication     | JWT Bearer — dev token via `POST /api/auth/dev-token`; production tokens from *Master* app |
| Rate limiting      | `AspNetCoreRateLimit`                             |
| Image compositing  | `SixLabors.ImageSharp`                            |
| API docs           | Swashbuckle / OpenAPI 3                           |

### Active data source

Product data is served from the **SQL Server database** (DB-first) with automatic fallback to **`LogoVisualizer.Api/Data/Midocean-print-data.json`** if the database is unreachable or empty. This is handled by `ProductDataService`. The raw Midocean endpoints (`GET /api/midocean-products`) always read from JSON.

The database is seeded on first run by a Docker `seeder` container (`SEED_AND_EXIT=true`) that applies EF Core migrations and imports all Midocean products with their print zones and per-zone image URLs.

---

## Solution Structure

```
LogoVisualizer.sln
├── LogoVisualizer.Api          → Web API entry point
│   ├── Controllers/
│   │   ├── AuthController.cs              ← POST /api/auth/dev-token (Development only)
│   │   ├── MidoceanProductsController.cs  ← raw JSON endpoints + adapted endpoints (DB-first via ProductDataService)
│   │   ├── ProductsController.cs          ← DB-backed CRUD
│   │   ├── PrintZonesController.cs        ← DB-backed zone CRUD (accepts AllowedTechniqueNames)
│   │   ├── TechniquesController.cs        ← DB-backed techniques read
│   │   ├── LogoUploadController.cs        ← logo upload
│   │   ├── FilesController.cs             ← serves uploaded files from uploads/
│   │   └── ExportController.cs           ← PNG composite
│   ├── Data/                   → Static data files (Midocean-print-data.json, midocean-top10.json)
│   ├── DTOs/                   → Request/response record types (incl. AdaptedProductDto)
│   ├── Extensions/             → IApplicationBuilder extension helpers
│   ├── Helpers/                → Utility/helper classes
│   ├── Properties/
│   │   └── launchSettings.json → Forces ASPNETCORE_ENVIRONMENT=Development
│   ├── Services/               → Service interfaces + implementations
│   └── uploads/                → Runtime file upload storage
└── LogoVisualizer.Data         → EF Core context, entity models, repositories
    └── Migrations/             → Applied — InitialCreate, AddAuditLogTable, AddPrintZoneImageUrl
```

`LogoVisualizer.Api` references `LogoVisualizer.Data`.

---

## Data Model

### EF Core entities (active — SQL Server via Docker)

```
Product
  ├── Id, Title, ImagePath, ImageWidth, ImageHeight
  ├── CreatedAt, UpdatedAt
  └── PrintZones[]
        ├── Id, ProductId, Name
        ├── X, Y, Width, Height          ← pixel coords on product image
        ├── MaxPhysicalWidthMm, MaxPhysicalHeightMm
        ├── MaxColors (nullable)
        ├── ImageUrl (nullable)          ← blank product photo for this print position
        └── AllowedTechniques[]          ← many-to-many via PrintZoneTechnique
              └── PrintTechnique { Id, Name, Description }
```

### Adapted DTOs (active, served from Midocean JSON)

```
AdaptedProductDto
  ├── Id, Title, ImageUrl, ImageWidth, ImageHeight
  └── PrintZones: AdaptedPrintZoneDto[]
        ├── Id, Name
        ├── X, Y, Width, Height          ← derived from Midocean point arrays
        ├── MaxPhysicalWidthMm, MaxPhysicalHeightMm
        ├── AllowedTechniques: string[]  ← mapped from Midocean technique codes
        └── ImageUrl                     ← per-position blank image, pinned to item_color_numbers[0]
```

`ImageUrl` on each zone is the blank product photo for that print position (FRONT, BACK, CHEST, etc.)
using the primary colour from `item_color_numbers[0]`, so all zones show the same colour variant.

Technique code mapping (`MapTechnique()` in `MidoceanProductService`):
`TR/ST1/SP` → `screen_print`, `E/EM` → `embroidery`, `EN/B` → `engraving`,
`SL/SA` → `sublimation`, `DTG/TDT/TT` → `digital_print`, `TP/P` → `pad_print`.

---

## Authentication & Authorisation

- **Admin write endpoints** (`POST`, `PUT`, `DELETE`) require `[Authorize]`. JWT tokens come from the external *Master* application — configured via `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key` in appsettings / user-secrets.
- **Development shortcut**: `POST /api/auth/dev-token` issues a 1-year JWT signed with the dev key. Only available when `ASPNETCORE_ENVIRONMENT=Development`. The admin frontend (`productApi.ts`) calls this automatically on the first write operation via `ensureToken()`.
- **Public read and viewer endpoints** have no `[Authorize]` attribute.
- **Upload and export endpoints** are public but rate-limited via `IpRateLimiting` config.

---

## Naming & Coding Conventions

- **Namespaces**: `LogoVisualizer.Api` and `LogoVisualizer.Data`
- **DTOs**: C# `record` types with positional constructors. Static `FromEntity()` factory methods on response DTOs.
- **Controllers**: inject repository interfaces, not `AppDbContext` directly (exception: `TechniquesController` and `PrintZonesController` use `AppDbContext` for specific EF queries).
- All async methods accept a `CancellationToken ct` parameter.
- Use `NotFound()`, `BadRequest(new { error = "..." })`, `ValidationProblem()`, `NoContent()`, `CreatedAtAction()` — never throw HTTP exceptions.
- File uploads use `Guid.NewGuid().ToString("N")` as filename — never the user-supplied name.

---

## Key Files

| File | Purpose |
|------|---------|
| `LogoVisualizer.Api/Program.cs` | DI registration, middleware pipeline — applies migrations + seeds on Development startup; `SEED_AND_EXIT=true` runs seeder-only mode for Docker |
| `LogoVisualizer.Api/Properties/launchSettings.json` | Forces `ASPNETCORE_ENVIRONMENT=Development`; Swagger always available |
| `LogoVisualizer.Api/appsettings.Development.json` | Connection string pointing to Docker SQL Server (`localhost,1433;Database=LogoVisualizer`), dev JWT key |
| `docker-compose.yml` | Starts `mssql` (SQL Server 2022) and `seeder` (one-shot migration + seed container) |
| `Dockerfile` | Multi-stage build for the API; also used by the seeder service |
| `LogoVisualizer.Data/AppDbContext.cs` | EF model config, index constraints, technique seed data |
| `LogoVisualizer.Data/Repositories/` | `IProductRepository`, `IPrintZoneRepository` + implementations |
| `LogoVisualizer.Api/Controllers/AuthController.cs` | `POST /api/auth/dev-token` — issues dev JWT (Development only) |
| `LogoVisualizer.Api/Controllers/MidoceanProductsController.cs` | Raw JSON endpoints; adapted endpoints delegate to `ProductDataService` (DB-first) |
| `LogoVisualizer.Api/Controllers/PrintZonesController.cs` | Zone CRUD — accepts `AllowedTechniqueNames` (string names) resolved via `ResolveTechniquesAsync` |
| `LogoVisualizer.Api/Controllers/ExportController.cs` | PNG composite generation using ImageSharp |
| `LogoVisualizer.Api/Controllers/LogoUploadController.cs` | Logo file upload; SVG sanitisation is a TODO |
| `LogoVisualizer.Api/Services/ProductDataService.cs` | `IProductDataService` — queries DB, falls back to JSON on error/empty |
| `LogoVisualizer.Api/Services/MidoceanProductService.cs` | Singleton — loads `Midocean-print-data.json`, adapts to frontend shape (JSON fallback) |
| `LogoVisualizer.Api/Services/MidoceanSeederService.cs` | Seeds DB from `Midocean-print-data.json`; idempotent (skips if products exist); stores per-zone `ImageUrl` |
| `LogoVisualizer.Api/Data/Midocean-print-data.json` | Full Midocean supplier feed used for seeding and JSON fallback |
| `LogoVisualizer.Api/DTOs/MidoceanDtos.cs` | Raw Midocean DTO types + `AdaptedProductDto` / `AdaptedPrintZoneDto` |

---

## Common Tasks

### Start the database + seed
```bash
cd backend-bachelor-logo-visualiser
docker compose up -d
# Starts mssql, waits for healthy, then runs seeder (applies migrations + seeds data)
```

### Run the API locally
```bash
cd LogoVisualizer.Api
dotnet run
# Swagger: http://localhost:5000/swagger
# On first run: applies any pending migrations, skips seeding (data already in DB)
```

### Wipe and re-seed from scratch
```bash
docker compose down -v   # destroys the sql_data volume
docker compose build     # rebuild seeder image if code changed
docker compose up -d     # fresh DB + seed
```

### Add an EF Core migration
```bash
dotnet ef migrations add <Name> \
  --project LogoVisualizer.Data \
  --startup-project LogoVisualizer.Api
```

### Set dev secrets (JWT key)
```bash
cd LogoVisualizer.Api
dotnet user-secrets set "Jwt:Key" "<value>"
```

---

## Known TODOs / Open Items

- **Image dimensions assumed**: Midocean CDN images are assumed to be 1000×1000 px. Actual dimensions should be detected or fetched if they differ.
- **SVG sanitisation**: `LogoUploadController` logs a warning but does not yet strip `<script>` tags from SVG uploads. Add an SVG sanitiser before production.
- **Auth integration with Master**: Replace the dev JWT config with the actual Master application issuer/audience/key. The `dev-token` endpoint must not be exposed in production.
- **PDF export** (nice-to-have): A multi-page PDF with front/back side is not yet implemented.

---

## Out of Scope (do not implement)

- 3D rendering
- PMS colour matching
- Production file generation (CMYK separations, embroidery stitch files)
- Shopify / external e-commerce integration
- Automatic logo vectorisation
