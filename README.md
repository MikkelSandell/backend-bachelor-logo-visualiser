# LogoVisualizer — Backend

REST API backend for the **Logo Visualizer & Product Setup Tool** — a service that lets end-customers preview their logo on promotional merchandise products, and lets internal admins configure those products' print zones.

## Prerequisites

| Tool | Version | Link |
|------|---------|------|
| .NET SDK | 8.x | https://dotnet.microsoft.com/download/dotnet/8.0 |
| SQL Server | 2019+ or LocalDB | https://www.microsoft.com/sql-server/sql-server-downloads |
| Visual Studio 2022 or VS Code | latest | — |

> **Note:** The .NET *runtime* alone is not enough — you need the full **SDK** to build and run migrations.

---

## Project Structure

```
backend/
├── LogoVisualizer.sln
├── global.json                        # pins .NET 8 SDK
│
├── LogoVisualizer.Api/                # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── ProductsController.cs      # CRUD + import/export
│   │   ├── PrintZonesController.cs    # CRUD per product
│   │   ├── TechniquesController.cs    # lookup (read-only)
│   │   ├── LogoUploadController.cs    # logo file upload
│   │   └── ExportController.cs       # PNG composite export
│   ├── DTOs/                          # request / response records
│   ├── Extensions/
│   │   └── MigrationExtensions.cs    # auto-migrate on dev startup
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── uploads/                       # runtime upload storage (git-ignored)
│
└── LogoVisualizer.Data/               # EF Core data layer
    ├── Models/
    │   ├── Product.cs
    │   ├── PrintZone.cs
    │   ├── PrintTechnique.cs
    │   └── PrintZoneTechnique.cs      # many-to-many join
    ├── Repositories/
    │   ├── IProductRepository.cs
    │   ├── ProductRepository.cs
    │   ├── IPrintZoneRepository.cs
    │   └── PrintZoneRepository.cs
    └── AppDbContext.cs
```

---

## Setup — First Run

### 1. Install the .NET 8 SDK

Download and install from https://dotnet.microsoft.com/download/dotnet/8.0, then confirm:

```powershell
dotnet --version   # should print 8.x.x
```

### 2. Configure the database connection

Edit `LogoVisualizer.Api/appsettings.Development.json` (or use user-secrets):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LogoVisualizer_Dev;Trusted_Connection=True;"
  }
}
```

For a full SQL Server instance, change the connection string accordingly.

### 3. Configure JWT (authentication from Master app)

The admin endpoints use JWT tokens issued by the existing **Master** application.
Set the matching issuer/audience/key in your dev secrets:

```powershell
cd LogoVisualizer.Api
dotnet user-secrets set "Jwt:Issuer"   "MasterApp"
dotnet user-secrets set "Jwt:Audience" "LogoVisualizer"
dotnet user-secrets set "Jwt:Key"      "<same-secret-as-Master>"
```

> Never commit the real key. In production, use an environment variable `Jwt__Key`.

### 4. Apply database migrations

```powershell
cd LogoVisualizer.Api
dotnet ef database update
```

This creates the database and seeds the standard print techniques (Screen Print, Embroidery, Sublimation, etc.).

### 5. Run the API

```powershell
dotnet run --project LogoVisualizer.Api
```

Open **Swagger UI**: `https://localhost:5001/swagger`

---

## API Endpoint Reference

| Method   | Path                                    | Auth  | Description                               |
|----------|-----------------------------------------|-------|-------------------------------------------|
| `GET`    | `/api/products`                         | —     | List all products (with status)           |
| `GET`    | `/api/products/{id}`                    | —     | Get product + print zones + techniques    |
| `POST`   | `/api/products`                         | JWT   | Create product (multipart: image + meta)  |
| `PUT`    | `/api/products/{id}`                    | JWT   | Update product metadata                   |
| `DELETE` | `/api/products/{id}`                    | JWT   | Delete product                            |
| `POST`   | `/api/products/import`                  | JWT   | Import from supplier JSON array           |
| `GET`    | `/api/products/{id}/export`             | JWT   | Export product as JSON                    |
| `GET`    | `/api/products/{productId}/zones`       | —     | List print zones for a product            |
| `GET`    | `/api/products/{productId}/zones/{id}`  | —     | Get single print zone                     |
| `POST`   | `/api/products/{productId}/zones`       | JWT   | Create print zone                         |
| `PUT`    | `/api/products/{productId}/zones/{id}`  | JWT   | Update print zone                         |
| `DELETE` | `/api/products/{productId}/zones/{id}`  | JWT   | Delete print zone                         |
| `GET`    | `/api/techniques`                       | —     | List all print techniques (lookup)        |
| `POST`   | `/api/logos/upload`                     | —     | Upload logo file → returns `logoId`       |
| `POST`   | `/api/export/png`                       | —     | Composite PNG from product + logo         |

All write endpoints respond `401 Unauthorized` without a valid Bearer token.
Public endpoints are rate-limited (see `IpRateLimiting` in `appsettings.json`).

Full OpenAPI spec is available at `/swagger/v1/swagger.json`.

---

## Adding / Running Migrations

```powershell
# From the backend/ folder:
dotnet ef migrations add <MigrationName> `
    --project LogoVisualizer.Data `
    --startup-project LogoVisualizer.Api

dotnet ef database update --project LogoVisualizer.Data --startup-project LogoVisualizer.Api
```

---

## Product Import JSON Format

`POST /api/products/import` accepts a JSON array:

```json
[
  {
    "title": "Classic Cotton T-Shirt",
    "imageUrl": "https://supplier.example/images/tshirt.png",
    "imageWidth": 1200,
    "imageHeight": 1600,
    "printZones": [
      {
        "name": "forside",
        "x": 300,
        "y": 200,
        "width": 600,
        "height": 700,
        "maxPhysicalWidthMm": 300,
        "maxPhysicalHeightMm": 350,
        "maxColors": 8,
        "allowedTechniques": ["Screen Print", "DTG", "Sublimation"]
      }
    ]
  }
]
```

---

## Security Notes

- **JWT keys** must never be committed. Use `dotnet user-secrets` in dev and environment variables in production.
- **SVG uploads** are accepted but not yet sanitised. Before going to production, add an SVG sanitiser to strip embedded scripts from logo uploads.
- **File uploads** are stored with GUID-based filenames — user-supplied filenames are never used on disk.
- `appsettings.Production.json` is git-ignored; configure production settings via environment variables or a secrets manager.
