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
| ORM                | Entity Framework Core 10 (not active — no DB yet) |
| Database           | Microsoft SQL Server — **not in use, no DB installed** |
| Authentication     | JWT Bearer — tokens issued by *Master* app (wired, not active) |
| Rate limiting      | `AspNetCoreRateLimit`                             |
| Image compositing  | `SixLabors.ImageSharp`                            |
| API docs           | Swashbuckle / OpenAPI 3                           |

### Active data source

All product data is currently served from **`LogoVisualizer.Api/Data/midocean-top10.json`** via `MidoceanProductService`. The DB-backed endpoints exist in code but require a database connection to function. `app.ApplyMigrations()` is commented out in `Program.cs`.

---

## Solution Structure

```
LogoVisualizer.sln
├── LogoVisualizer.Api          → Web API entry point
│   ├── Controllers/
│   │   ├── MidoceanProductsController.cs  ← active (no DB needed)
│   │   ├── ProductsController.cs          ← DB-backed (requires DB)
│   │   ├── PrintZonesController.cs        ← DB-backed (requires DB)
│   │   ├── TechniquesController.cs        ← DB-backed (requires DB)
│   │   ├── LogoUploadController.cs        ← logo upload
│   │   └── ExportController.cs           ← PNG composite
│   ├── Data/                   → Static data files (midocean-top10.json)
│   ├── DTOs/                   → Request/response record types (incl. AdaptedProductDto)
│   ├── Extensions/             → IApplicationBuilder extension helpers
│   ├── Helpers/                → Utility/helper classes
│   ├── Properties/
│   │   └── launchSettings.json → Forces ASPNETCORE_ENVIRONMENT=Development
│   ├── Services/               → Service interfaces + implementations
│   └── uploads/                → Runtime file upload storage
└── LogoVisualizer.Data         → EF Core context, entity models, repositories
    └── Migrations/             → Created but not yet applied (no DB)
```

`LogoVisualizer.Api` references `LogoVisualizer.Data`.

---

## Data Model

### EF Core entities (DB-backed, not active)

```
Product
  ├── Id, Title, ImagePath, ImageWidth, ImageHeight
  ├── CreatedAt, UpdatedAt
  └── PrintZones[]
        ├── Id, ProductId, Name
        ├── X, Y, Width, Height          ← pixel coords on product image
        ├── MaxPhysicalWidthMm, MaxPhysicalHeightMm
        ├── MaxColors (nullable)
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
| `LogoVisualizer.Api/Program.cs` | DI registration, middleware pipeline — `ApplyMigrations()` is commented out |
| `LogoVisualizer.Api/Properties/launchSettings.json` | Forces `ASPNETCORE_ENVIRONMENT=Development`; Swagger always available |
| `LogoVisualizer.Api/appsettings.Development.json` | LocalDB connection string (for future use), dev JWT key |
| `LogoVisualizer.Data/AppDbContext.cs` | EF model config, index constraints, technique seed data |
| `LogoVisualizer.Data/Repositories/` | `IProductRepository`, `IPrintZoneRepository` + implementations |
| `LogoVisualizer.Api/Controllers/MidoceanProductsController.cs` | Active endpoints — raw and adapted Midocean products |
| `LogoVisualizer.Api/Controllers/ExportController.cs` | PNG composite generation using ImageSharp |
| `LogoVisualizer.Api/Controllers/LogoUploadController.cs` | Logo file upload; SVG sanitisation is a TODO |
| `LogoVisualizer.Api/Services/IMidoceanProductService.cs` | Interface — `GetAll()`, `GetByMasterCode()`, `GetAllAdapted()`, `GetAdaptedByMasterCode()` |
| `LogoVisualizer.Api/Services/MidoceanProductService.cs` | Singleton — loads `midocean-top10.json`, adapts to frontend shape |
| `LogoVisualizer.Api/Data/midocean-top10.json` | 10 Midocean products extracted from the full feed |
| `LogoVisualizer.Api/DTOs/MidoceanDtos.cs` | Raw Midocean DTO types + `AdaptedProductDto` / `AdaptedPrintZoneDto` |

---

## Common Tasks

### Run locally
```bash
cd LogoVisualizer.Api
dotnet run
# Swagger: http://localhost:5000/swagger
```

### Add a migration (for when DB is ready)
```bash
dotnet ef migrations add <Name> \
  --project LogoVisualizer.Data \
  --startup-project LogoVisualizer.Api
dotnet ef database update \
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

- **No database**: `app.ApplyMigrations()` is commented out in `Program.cs`. Migrations exist under `LogoVisualizer.Data/Migrations/` — uncomment when LocalDB or SQL Server is available.
- **Image dimensions assumed**: Midocean CDN images are assumed to be 1000×1000 px. Actual dimensions should be detected or fetched if they differ.
- **SVG sanitisation**: `LogoUploadController` logs a warning but does not yet strip `<script>` tags from SVG uploads. Add an SVG sanitiser before production.
- **Auth integration with Master**: The JWT config is set up generically. Once you have the Master app's actual issuer/audience/key, set those values in user-secrets and confirm token validation works end-to-end.
- **File serving route**: Uploaded files are stored under `uploads/` but there is no `/api/files/` controller yet. Add a `FilesController` that serves files from the upload directory with correct content-type headers.
- **PDF export** (nice-to-have): A multi-page PDF with front/back side is not yet implemented.

---

## Out of Scope (do not implement)

- 3D rendering
- PMS colour matching
- Production file generation (CMYK separations, embroidery stitch files)
- Shopify / external e-commerce integration
- Automatic logo vectorisation
