using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagement.Web.Data.Migrations
{
    /// <summary>
    /// Creates the two report procedures. EF Core doesn't track stored procedures in the
    /// model snapshot, so they are hand-written here and any later change needs a new migration.
    /// The column names returned must match the report result records exactly, because
    /// EF maps procedure results to records by name at runtime.
    /// </summary>
    public partial class AddReportStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Average price and product count per category, sorted by category name.
            // AVG over decimal(18,2) returns decimal(38,6), so it is rounded and cast back to
            // the Price type to give a money value with 2 decimals.
            // An empty Products table returns no rows, which the Report page shows as its empty state.
            migrationBuilder.Sql("""
                CREATE PROCEDURE dbo.usp_GetAveragePriceByCategory
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT  Category,
                            CAST(ROUND(AVG(Price), 2) AS decimal(18, 2)) AS AveragePrice,
                            COUNT(*)                                     AS ProductCount
                    FROM    dbo.Products
                    GROUP BY Category
                    ORDER BY Category;
                END
                """);

            // The category whose products have the highest total stock value, where stock value
            // is Price x Stock (money tied up in stock), not the number of units.
            // WITH TIES returns every category that shares the top total, so a tie isn't decided
            // arbitrarily by row order.
            migrationBuilder.Sql("""
                CREATE PROCEDURE dbo.usp_GetHighestStockValueCategory
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT TOP (1) WITH TIES
                            Category,
                            SUM(Price * Stock) AS TotalStockValue
                    FROM    dbo.Products
                    GROUP BY Category
                    ORDER BY SUM(Price * Stock) DESC;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetHighestStockValueCategory;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetAveragePriceByCategory;");
        }
    }
}
