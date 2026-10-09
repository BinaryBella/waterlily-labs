using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagement.Web.Data.Migrations
{
    /// <summary>
    /// Inserts sample products so both pages show data on first run. The values are chosen so
    /// the report results are easy to check by hand:
    ///   Electronics: avg (25 + 35 + 60) / 3   = 40.00,  stock value 1000 + 700 + 600 = 2300
    ///   Furniture:   avg (150 + 400) / 2      = 275.00, stock value 1200 + 1200      = 2400 (highest)
    ///   Stationery:  avg (2.5 + 4 + 12.5) / 3 = 6.33,   stock value 500 + 600 + 300  = 1400
    /// Furniture has the fewest units but the highest value, which shows that the report
    /// ranks by Price x Stock rather than by unit count.
    /// </summary>
    public partial class SeedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ProductId is omitted so the identity column generates it.
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Name", "Category", "Price", "Stock" },
                values: new object[,]
                {
                    { "Wireless Mouse",    "Electronics", 25.00m,  40 },
                    { "USB-C Charger",     "Electronics", 35.00m,  20 },
                    { "Bluetooth Speaker", "Electronics", 60.00m,  10 },
                    { "Office Chair",      "Furniture",   150.00m, 8 },
                    { "Standing Desk",     "Furniture",   400.00m, 3 },
                    { "Notebook",          "Stationery",  2.50m,   200 },
                    { "Gel Pen Pack",      "Stationery",  4.00m,   150 },
                    { "Desk Organizer",    "Stationery",  12.50m,  24 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The seeded IDs are generated, so the rows are matched by name and category.
            // Seeded rows that were edited in the app are left alone.
            migrationBuilder.Sql("""
                DELETE FROM dbo.Products
                WHERE  (Name = N'Wireless Mouse'    AND Category = N'Electronics')
                    OR (Name = N'USB-C Charger'     AND Category = N'Electronics')
                    OR (Name = N'Bluetooth Speaker' AND Category = N'Electronics')
                    OR (Name = N'Office Chair'      AND Category = N'Furniture')
                    OR (Name = N'Standing Desk'     AND Category = N'Furniture')
                    OR (Name = N'Notebook'          AND Category = N'Stationery')
                    OR (Name = N'Gel Pen Pack'      AND Category = N'Stationery')
                    OR (Name = N'Desk Organizer'    AND Category = N'Stationery');
                """);
        }
    }
}
