# Product Management

A small ASP.NET Core MVC application for managing products, backed by SQL Server through EF Core code-first.

- **Products** (`/Products`, also the home page): list, add, edit and delete products. Add and Edit open in a modal and save with AJAX; Delete asks for confirmation.
- **Report** (`/Reports`): the average price of products in each category, and the category with the highest stock value. Both are calculated by stored procedures.

The design and the reasoning behind it are in [`docs/PLAN.md`](docs/PLAN.md).

## Requirements

- .NET 10 SDK
- SQL Server (developed against SQL Server 2022, running locally)
- The EF Core CLI tool, version 10:

  ```
  dotnet tool install --global dotnet-ef
  ```

  If an older version is installed, run `dotnet tool update --global dotnet-ef`.

## Setup

1. **Connection string.** `ProductManagement.Web/appsettings.json` holds the connection string *without a password*:

   ```
   Server=localhost,1433;Database=ProductManagement;User Id=sa;TrustServerCertificate=True
   ```

   Supply the full string through user secrets, so the password stays out of source control:

   ```
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=ProductManagement;User Id=sa;Password=<your password>;TrustServerCertificate=True" --project ProductManagement.Web
   ```

   With Windows authentication, use `Server=localhost;Database=ProductManagement;Trusted_Connection=True;TrustServerCertificate=True` instead. User secrets are only read in the Development environment, which is what `dotnet run` uses.

2. **Database.** Create the database, table, stored procedures and sample data:

   ```
   dotnet ef database update --project ProductManagement.Web
   ```

   The whole database is built by three migrations, so it can be dropped and rebuilt from code at any time:

   | Migration | What it does |
   | --- | --- |
   | `InitialCreate` | Products table, check constraints (Price and Stock not negative), index on Category |
   | `AddReportStoredProcedures` | `dbo.usp_GetAveragePriceByCategory` and `dbo.usp_GetHighestStockValueCategory` |
   | `SeedProducts` | 8 sample products in 3 categories |

3. **Run.**

   ```
   dotnet run --project ProductManagement.Web
   ```

   Then open http://localhost:5118.

## Tests

```
dotnet test
```

The 42 MSTest tests need no database. `ProductServiceTests` and `ReportServiceTests` replace the repository and the cache with Moq mocks. `MemoryCacheServiceTests` runs the real cache with a fake clock to check expiry. To run one group:

```
dotnet test --filter "FullyQualifiedName~ProductServiceTests.CreateAsync_"
```

## How it is built

```
Controller  →  Service  →  Repository  →  EF Core  →  SQL Server
                  │
                  └── ICacheService (IMemoryCache)
```

| Folder (in `ProductManagement.Web`) | Responsibility |
| --- | --- |
| `Controllers/` | HTTP only. `ProductsController` depends on `IProductService`, `ReportsController` on `IReportService`. Neither touches the database. |
| `Services/` | Business rules: trimming and validation, mapping entities to immutable DTO records, caching, and clearing the cache after writes. |
| `Services/Caching/` | `ICacheService`, `MemoryCacheService` and `CacheKeys`. |
| `Repositories/` | The only code that uses `AppDbContext`. All methods are async and take a `CancellationToken`; reads use `AsNoTracking`. |
| `Data/` | `AppDbContext`, the Fluent API mapping in `Configurations/`, and the migrations. |
| `Models/` | `Entities/` (the EF entity), `Dtos/` (what services return), `ViewModels/` (forms and pages). |

**Stored procedures.** Both report calculations run in SQL; the repository calls the procedures through EF Core raw SQL and maps each row to a record by column name.

- `usp_GetAveragePriceByCategory`: `AVG(Price)` rounded to 2 decimals, and the product count, per category, sorted by category.
- `usp_GetHighestStockValueCategory`: `SUM(Price * Stock)` per category, `TOP (1) WITH TIES`, so tied categories are all returned.

**Caching.** `ICacheService` has two generic methods. Each takes a cache key and a `Func<Task<T>>` delegate that loads the data; the delegate is only called on a cache miss. A `null` result ("not found") is never cached.

| Method | Lifetime | Used for |
| --- | --- | --- |
| `CachedLongAsync<T>` | No expiry, kept until removed | Product list, category list |
| `CachedAsync<T>` | 5 minutes, absolute | Single product, both reports |

**Delegates.** Besides the cache-miss callback above, `ProductService` holds a multicast delegate, `ProductChangedHandler(int productId)`, invoked after every successful create, update or delete. Its two handlers remove the product list, category list and that product's entry, and both report entries, so the next read loads fresh data. It returns `void` on purpose: a multicast delegate only returns its last handler's result, so async handlers would have their earlier tasks dropped.

**Products page.** jQuery loads the Add/Edit form as a partial view into a Bootstrap modal and re-parses it for jQuery Validation each time. The server replies with a status code that the script acts on:

| Response | Meaning |
| --- | --- |
| 200 + table rows | Saved or deleted; the table is replaced |
| 422 + form | Validation failed; the form is shown again with its messages |
| 404 | The product no longer exists; a message is shown and the table refreshed |
| 400 | Missing or invalid anti-forgery token |

Every POST carries an anti-forgery token. The Save button is disabled while a request is in flight. The Category field suggests existing categories.

## Sample data and expected report

| Category | Products (Price × Stock) | Average price | Stock value |
| --- | --- | --- | --- |
| Electronics | 25.00 × 40, 35.00 × 20, 60.00 × 10 | 40.00 | 2,300.00 |
| Furniture | 150.00 × 8, 400.00 × 3 | 275.00 | **2,400.00** (highest) |
| Stationery | 2.50 × 200, 4.00 × 150, 12.50 × 24 | 6.33 | 1,400.00 |

Furniture has the fewest units (11) but the highest stock value, which shows the report ranks by money tied up in stock rather than by unit count.

## Assumptions and known limitations

- **Stock value** is Price × Stock summed per category, not the number of units. If two categories share the top value, both are shown.
- **Category is free text**, as the brief rules out a category table. Values are trimmed, and SQL Server's default case-insensitive collation groups "Toys" and "toys" together, but typos are not merged. The Category field's suggestions help avoid them.
- **The cache is in-process**, so it suits a single server instance. Writes made through the application clear the affected entries immediately. Changes made directly in the database are not seen by the product list until the application restarts, and reach the reports within 5 minutes.
- No authentication, and no pages beyond Products and Report, as the brief asks.

## Departures from the plan

The build follows `docs/PLAN.md`. These details were added or settled during development:

- **`ProductInput` record** for the values submitted to create or update. Services stay independent of the web form models.
- **`ServiceResult<T>`** reports Success, Invalid (with an error per field) or NotFound, so expected outcomes aren't exceptions.
- **`IProductRepository.GetCategoriesAsync`** and **`ICacheService.Remove`**, needed by the Category suggestions and the cache invalidation in the plan's pseudo code.
- **`CacheKeys.Product(id)`** builds the single-product key, since it includes the ID; all other keys are constants.
- **`ErrorController`** serves the shared error page outside Development; **`GET /Products/Rows`** refreshes the table after a "not found".
- **`ReportServiceTests`** (3 tests) in addition to the planned `ProductService` and `MemoryCacheService` tests.
