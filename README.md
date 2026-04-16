# LogoVisualizer — Backend

REST API backend for the **Logo Visualizer & Product Setup Tool** — a service that lets end-customers preview their logo on promotional merchandise products, and lets internal admins configure those products' print zones.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.x | https://dotnet.microsoft.com/download/dotnet/10.0 |
| SQL Server / LocalDB | 2019+ | **Optional — not required for current setup** |

> **Note:** The .NET *runtime* alone is not enough — you need the full **SDK** to build and run migrations.

### Current data source

The app currently runs **without a database**. All product data is served from
`LogoVisualizer.Api/Data/midocean-top10.json` via the `MidoceanProductService`.
The DB-backed product/zone CRUD endpoints exist in code but will return errors until a database is configured.
See [Setting up the database](#setting-up-the-database-optional) below.

---

## Project structure

```
backend-bachelor-logo-visualiser/
├── LogoVisualizer.sln
├── global.json                              # pins .NET 10 SDK
│
├── LogoVisualizer.Api/                      # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── MidoceanProductsController.cs   # Midocean JSON data (active)
│   │   ├── ProductsController.cs           # DB-backed CRUD (requires DB)
│   │   ├── PrintZonesController.cs         # DB-backed CRUD (requires DB)
│   │   ├── TechniquesController.cs         # lookup (requires DB)
│   │   ├── LogoUploadController.cs         # logo file upload
│   │   └── ExportController.cs            # PNG composite export
│   ├── DTOs/                               # request / response records
│   ├── Extensions/
│   │   └── MigrationExtensions.cs         # auto-migrate helper (skipped until DB ready)
│   ├── Properties/
│   │   └── launchSettings.json            # sets ASPNETCORE_ENVIRONMENT=Development
│   ├── Services/
│   │   ├── IMidoceanProductService.cs
│   │   └── MidoceanProductService.cs      # loads midocean-top10.json at startup
│   ├── Data/
│   │   └── midocean-top10.json            # 10 Midocean supplier products
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── uploads/                            # runtime upload storage (git-ignored)
│
└── LogoVisualizer.Data/                    # EF Core data layer
    ├── Models/
    │   ├── Product.cs
    │   ├── PrintZone.cs
    │   ├── PrintTechnique.cs
    │   └── PrintZoneTechnique.cs           # many-to-many join
    ├── Repositories/
    │   ├── IProductRepository.cs
    │   ├── ProductRepository.cs
    │   ├── IPrintZoneRepository.cs
    │   └── PrintZoneRepository.cs
    ├── Migrations/                          # EF Core migrations (created, not yet applied)
    └── AppDbContext.cs
```

---

## Running the API

```powershell
cd LogoVisualizer.Api
dotnet run
```

- API base: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

`launchSettings.json` automatically sets `ASPNETCORE_ENVIRONMENT=Development` so Swagger is always available.

---

## API Endpoint Reference

### Midocean products — active, no DB required

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/midocean-products` | — | All 10 products in raw Midocean format |
| `GET` | `/api/midocean-products/{masterCode}` | — | Single product by `master_code` (e.g. `S11500`) |
| `GET` | `/api/midocean-products/as-products` | — | All 10 products adapted to the frontend `Product` shape |
| `GET` | `/api/midocean-products/{masterCode}/as-product` | — | Single product adapted to the frontend `Product` shape |

### DB-backed product & zone endpoints — require database

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/products` | — | List all products (with status) |
| `GET` | `/api/products/{id}` | — | Get product + print zones |
| `POST` | `/api/products` | JWT | Create product |
| `PUT` | `/api/products/{id}` | JWT | Update product metadata |
| `DELETE` | `/api/products/{id}` | JWT | Delete product |
| `POST` | `/api/products/import` | JWT | Import from supplier JSON |
| `GET` | `/api/products/export` | JWT | Export all products as JSON |
| `GET` | `/api/products/{productId}/zones` | — | List print zones |
| `POST` | `/api/products/{productId}/zones` | JWT | Create print zone |
| `PUT` | `/api/products/{productId}/zones/{id}` | JWT | Update print zone |
| `DELETE` | `/api/products/{productId}/zones/{id}` | JWT | Delete print zone |
| `GET` | `/api/techniques` | — | List all print techniques |

### Upload & export

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/logos` | — | Upload logo file → returns URL |
| `POST` | `/api/export/png` | — | Composite PNG from product + logo |

Full OpenAPI spec: `/swagger/v1/swagger.json`

---

## Setting up the database (optional)

When you're ready to use the DB-backed endpoints:

### 1. Install SQL Server LocalDB

Download from https://www.microsoft.com/sql-server/sql-server-downloads (Express edition includes LocalDB).

### 2. Configure the connection string

`LogoVisualizer.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LogoVisualizer_Dev;Trusted_Connection=True;"
  }
}
```

### 3. Re-enable migrations on startup

In `Program.cs`, uncomment:

```csharp
app.ApplyMigrations();
```

Migrations are already created under `LogoVisualizer.Data/Migrations/` — they will run automatically on next startup.

### 4. Configure JWT (authentication from Master app)

```powershell
cd LogoVisualizer.Api
dotnet user-secrets set "Jwt:Issuer"   "MasterApp"
dotnet user-secrets set "Jwt:Audience" "LogoVisualizer"
dotnet user-secrets set "Jwt:Key"      "<same-secret-as-Master>"
```

---

## Adding migrations

```powershell
dotnet ef migrations add <MigrationName> `
    --project LogoVisualizer.Data `
    --startup-project LogoVisualizer.Api

dotnet ef database update `
    --project LogoVisualizer.Data `
    --startup-project LogoVisualizer.Api
```

---

## Security notes

- **JWT keys** must never be committed. Use `dotnet user-secrets` in dev.
- **SVG uploads** are accepted but not yet sanitised — add an SVG sanitiser before any user-facing deployment.
- **File uploads** use GUID-based filenames — the user-supplied filename is never written to disk.
