using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SIDAPA.infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedExportRequiredDocuments : Migration
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
                    { 11, (short)8, true, (short)2, null },
                    { 12, (short)9, true, (short)2, null },
                    { 13, (short)10, true, (short)2, null },
                    { 14, (short)11, false, (short)2, null },
                    { 15, (short)12, false, (short)2, null },
                    { 16, (short)13, false, (short)2, null },
                    { 17, (short)14, false, (short)2, null },
                    { 18, (short)16, false, (short)2, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 11);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 12);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 13);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 14);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 15);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 16);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 17);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_documento_requerido",
                keyColumn: "id_documento_requerido",
                keyValue: 18);
        }
    }
}
