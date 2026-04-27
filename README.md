# LogoVisualizer — Backend

REST API backend for the **Logo Visualizer & Product Setup Tool** — a service that lets end-customers preview their logo on promotional merchandise products, and lets internal admins configure those products' print zones.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.x | https://dotnet.microsoft.com/download/dotnet/10.0 |
| Docker Desktop | any recent | Required for SQL Server |

> **Note:** The .NET *runtime* alone is not enough — you need the full **SDK** to build and run migrations.

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
    ├── Migrations/                          # EF Core migrations (applied automatically on startup)
    └── AppDbContext.cs
```

---

## Running the API

### 1. Start the database

```powershell
cd backend-bachelor-logo-visualiser
docker compose up -d
```

This starts SQL Server 2022 on port 1433, then runs the seeder container which applies EF Core migrations and imports all Midocean products. Data is persisted in the `sql_data` Docker volume.

### 2. Start the API

```powershell
cd LogoVisualizer.Api
dotnet run
```

- API base: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

`launchSettings.json` automatically sets `ASPNETCORE_ENVIRONMENT=Development` so Swagger is always available.

If the database is unreachable, the API falls back to serving product data from `LogoVisualizer.Api/Data/Midocean-print-data.json` automatically.

---

## API Endpoint Reference

### Auth (Development only)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/auth/dev-token` | — | Issues a 1-year JWT with the `admin` role. Only available in `Development` environment. |

### Midocean products

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/midocean-products` | — | All products in raw Midocean format (JSON file) |
| `GET` | `/api/midocean-products/{masterCode}` | — | Single product by `master_code` |
| `GET` | `/api/midocean-products/as-products` | — | All products adapted to the frontend `Product` shape (DB-first, JSON fallback) |
| `GET` | `/api/midocean-products/{id}/as-product` | — | Single adapted product by DB id |

### Products & zones

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/products` | — | List all products (with status) |
| `GET` | `/api/products/{id}` | — | Get product + print zones |
| `POST` | `/api/products` | JWT | Create product |
| `PUT` | `/api/products/{id}` | JWT | Update product metadata + full zone list (creates/updates/deletes zones in one call) |
| `DELETE` | `/api/products/{id}` | JWT | Delete product and all its zones |
| `POST` | `/api/products/import` | JWT | Import products from JSON |
| `GET` | `/api/products/{id}/export` | JWT | Export a single product as JSON |
| `GET` | `/api/products/{productId}/zones` | — | List print zones |
| `POST` | `/api/products/{productId}/zones` | JWT | Create print zone |
| `PUT` | `/api/products/{productId}/zones/{id}` | JWT | Update print zone |
| `DELETE` | `/api/products/{productId}/zones/{id}` | JWT | Delete print zone |
| `GET` | `/api/techniques` | — | List all print techniques |

### Upload & export

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/logos/upload` | — | Upload logo file → returns `{ fileId, url }` |
| `POST` | `/api/export/png` | — | Composite logo onto product image → returns PNG |

Full OpenAPI spec: `/swagger/v1/swagger.json`

---

## Resetting the database

To wipe the database and re-seed from scratch:

```powershell
docker compose down -v   # destroys the sql_data volume
docker compose build     # rebuild seeder image if source code changed
docker compose up -d     # fresh DB + seed
```

## JWT configuration

In development, `POST /api/auth/dev-token` issues a token automatically — no manual setup needed. For production (tokens issued by the Master app):

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
