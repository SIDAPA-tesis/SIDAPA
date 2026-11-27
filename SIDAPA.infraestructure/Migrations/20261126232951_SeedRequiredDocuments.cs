using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SIDAPA.infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRequiredDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                columns: new[] { "id_documento_requerido", "id_tipo_documento", "obligatorio", "tipo_operacion", "clave_pedimento" },
                values: new object[,]
                {
                    { 1, (short)8, true, (short)1, null },
                    { 2, (short)9, true, (short)1, null },
                    { 3, (short)10, true, (short)1, null },
                    { 4, (short)7, false, (short)1, null },
                    { 5, (short)11, false, (short)1, null },
                    { 6, (short)12, false, (short)1, null },
                    { 7, (short)13, false, (short)1, null },
                    { 8, (short)14, false, (short)1, null },
                    { 9, (short)15, false, (short)1, null },
                    { 10, (short)16, false, (short)1, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 6);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 8);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 9);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 10);
        }
    }
}
