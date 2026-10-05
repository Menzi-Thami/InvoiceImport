using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceImport.Migrations
{
    /// <inheritdoc />
    public partial class UniqueInvoiceNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Earlier versions could insert the same invoice number twice. Stop with a clear
            // message instead of guessing which copy to delete; see README "Upgrading the schema".
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [InvoiceHeader] GROUP BY [InvoiceNumber] HAVING COUNT(*) > 1)
                BEGIN
                    ;THROW 50000, N'InvoiceHeader contains duplicate InvoiceNumber values. Resolve them before applying UniqueInvoiceNumber: SELECT InvoiceNumber, COUNT(*) FROM InvoiceHeader GROUP BY InvoiceNumber HAVING COUNT(*) > 1', 1;
                END
                IF EXISTS (SELECT 1 FROM [InvoiceHeader] WHERE LEN([InvoiceNumber]) > 50)
                BEGIN
                    ;THROW 50000, N'InvoiceHeader contains InvoiceNumber values longer than 50 characters. Resolve them before applying UniqueInvoiceNumber: SELECT InvoiceId, InvoiceNumber FROM InvoiceHeader WHERE LEN(InvoiceNumber) > 50', 1;
                END
                """);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "InvoiceHeader",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHeader_InvoiceNumber",
                table: "InvoiceHeader",
                column: "InvoiceNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceHeader_InvoiceNumber",
                table: "InvoiceHeader");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "InvoiceHeader",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
