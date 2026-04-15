# CLAUDE.md — LogoVisualizer Backend

This file gives Claude (and other AI coding assistants) the context needed to work effectively in this codebase without repeated explanation.

---

## What this project is

**LogoVisualizer** is an internal .NET 8 backend service for a promotional merchandise company. It allows:
- **End-customers** to upload their logo and preview it on a product (t-shirts, mugs, pens, etc.) within defined print zones.
- **Admin users** to set up products by uploading product images and drawing rectangular print zones, each with physical size limits and allowed print techniques.

The project is a **bachelor's thesis** deliverable. Code and documentation are in English; UI labels are in Danish.

---

## Tech Stack

| Layer              | Technology                                   |
|--------------------|----------------------------------------------|
| Web API framework  | ASP.NET Core 8 (controller-based)            |
| ORM                | Entity Framework Core 8                      |
| Database           | Microsoft SQL Server (LocalDB for dev)       |
| Authentication     | JWT Bearer — tokens issued by *Master* app   |
| Rate limiting      | `AspNetCoreRateLimit`                        |
| Image compositing  | `SixLabors.ImageSharp`                       |
| API docs           | Swashbuckle / OpenAPI 3                      |

---

## Solution Structure

```
LogoVisualizer.sln
├── LogoVisualizer.Api          → Web API entry point
│   ├── Controllers/            → HTTP controllers
│   ├── Data/                   → Static data files (e.g. midocean-top10.json)
│   ├── DTOs/                   → Request/response record types
│   ├── Extensions/             → IApplicationBuilder extension helpers
│   ├── Helpers/                → Utility/helper classes
│   ├── Services/               → Service interfaces + implementations
│   └── uploads/                → Runtime file upload storage
└── LogoVisualizer.Data         → EF Core context, entity models, repositories
```

`LogoVisualizer.Api` references `LogoVisualizer.Data`.

---

## Data Model

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

`PrintTechnique` rows are seeded in the initial migration (Screen Print, Embroidery, Sublimation, Engraving, DTG, Pad Print, Digital Print).

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
| `LogoVisualizer.Api/Program.cs` | DI registration, middleware pipeline |
| `LogoVisualizer.Api/appsettings.Development.json` | LocalDB connection string, dev JWT key |
| `LogoVisualizer.Data/AppDbContext.cs` | EF model config, index constraints, technique seed data |
| `LogoVisualizer.Data/Repositories/` | `IProductRepository`, `IPrintZoneRepository` + implementations |
| `LogoVisualizer.Api/Controllers/ExportController.cs` | PNG composite generation using ImageSharp |
| `LogoVisualizer.Api/Controllers/LogoUploadController.cs` | Logo file upload; SVG sanitisation is a TODO |
| `LogoVisualizer.Api/Controllers/MidoceanProductsController.cs` | Read-only endpoints for Midocean sample print data |
| `LogoVisualizer.Api/Services/IMidoceanProductService.cs` | Service interface for Midocean product data |
| `LogoVisualizer.Api/Services/MidoceanProductService.cs` | Singleton — loads `midocean-top10.json` once on startup |
| `LogoVisualizer.Api/Data/midocean-top10.json` | 10 richest Midocean products extracted from the full feed |

---

## Common Tasks

### Run locally
```bash
cd LogoVisualizer.Api
dotnet run
# Swagger: https://localhost:5001/swagger
```

### Add a migration
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

### SDK note
`global.json` uses `rollForward: latestMajor`, so the project builds with .NET 8 SDK or later (currently tested with .NET 10). The target framework remains `net8.0`.

---

## Known TODOs / Open Items

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
