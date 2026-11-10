using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SIDAPA.infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDocumentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "nombre",
                schema: "sidapa",
                table: "cat_tipo_documento",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);

            migrationBuilder.InsertData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                columns: new[] { "id_tipo_documento", "nombre" },
                values: new object[,]
                {
                    { (short)1, "Constancia de Situación Fiscal (RFC activo)" },
                    { (short)2, "Opinión de Cumplimiento de Obligaciones Fiscales (32-D positiva)" },
                    { (short)3, "Acta Constitutiva y Poder Notarial del Representante Legal" },
                    { (short)4, "Identificación Oficial del Representante Legal" },
                    { (short)5, "Comprobante de Domicilio Fiscal" },
                    { (short)6, "Encargo Conferido ante el SAT" },
                    { (short)7, "Registro en el Padrón de Importadores / Padrón de Sectores Específicos (solo importación)" },
                    { (short)8, "Factura Comercial / CFDI con Complemento de Comercio Exterior" },
                    { (short)9, "Lista de Empaque (Packing List)" },
                    { (short)10, "Documento de Transporte (B/L, Guía Aérea o Carta Porte)" },
                    { (short)11, "Certificado o Prueba de Origen" },
                    { (short)12, "Ficha Técnica / Catálogo / Hoja de Seguridad (MSDS)" },
                    { (short)13, "Permisos, Licencias y Certificados de RRNAs / NOMs" },
                    { (short)14, "Póliza o Certificado de Seguro de Transporte" },
                    { (short)15, "Manifestación de Valor y Hoja de Cálculo (solo importación)" },
                    { (short)16, "Comprobantes de Pago de Fletes y Gastos Incrementable" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)2);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)3);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)4);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)5);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)6);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)7);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)8);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)9);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)10);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)11);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)12);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)13);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)14);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)15);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_documento",
                keyColumn: "id_tipo_documento",
                keyValue: (short)16);

            migrationBuilder.AlterColumn<string>(
                name: "nombre",
                schema: "sidapa",
                table: "cat_tipo_documento",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);
        }
    }
}
