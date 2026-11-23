using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SIDAPA.infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AvisoArribo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "seq_numero_referencia",
                schema: "sidapa");

            migrationBuilder.InsertData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                columns: new[] { "clave", "clave_aduana", "vigente", "denominacion", "seccion", "fecha_actualizacion" },
                values: new object[,]
                {
                    { "010", "01", true, "ACAPULCO, ACAPULCO DE JUÁREZ, GUERRERO.", "0", null },
                    { "020", "02", true, "AGUA PRIETA, AGUA PRIETA, SONORA.", "0", null },
                    { "050", "05", true, "SUBTENIENTE LÓPEZ, OTHÓN P. BLANCO, QUINTANA ROO.", "0", null },
                    { "060", "06", true, "CIUDAD DEL CARMEN, CARMEN, CAMPECHE.", "0", null },
                    { "070", "07", true, "CIUDAD JUÁREZ, JUÁREZ, CHIHUAHUA.", "0", null },
                    { "080", "08", true, "COATZACOALCOS, COATZACOALCOS, VERACRUZ.", "0", null },
                    { "110", "11", true, "ENSENADA, ENSENADA, BAJA CALIFORNIA.", "0", null },
                    { "120", "12", true, "GUAYMAS, GUAYMAS, SONORA.", "0", null },
                    { "140", "14", true, "LA PAZ, LA PAZ, BAJA CALIFORNIA SUR.", "0", null },
                    { "160", "16", true, "MANZANILLO, MANZANILLO, COLIMA.", "0", null },
                    { "170", "17", true, "MATAMOROS, MATAMOROS, TAMAULIPAS.", "0", null },
                    { "180", "18", true, "MAZATLÁN, MAZATLÁN, SINALOA.", "0", null },
                    { "190", "19", true, "MEXICALI, MEXICALI, BAJA CALIFORNIA.", "0", null },
                    { "200", "20", true, "MÉXICO, CIUDAD DE MÉXICO.", "0", null },
                    { "220", "22", true, "NACO, NACO, SONORA.", "0", null },
                    { "230", "23", true, "NOGALES, NOGALES, SONORA.", "0", null },
                    { "240", "24", true, "NUEVO LAREDO, NUEVO LAREDO, TAMAULIPAS.", "0", null },
                    { "250", "25", true, "OJINAGA, OJINAGA, CHIHUAHUA.", "0", null },
                    { "260", "26", true, "PUERTO PALOMAS, ASCENSIÓN, CHIHUAHUA.", "0", null },
                    { "270", "27", true, "PIEDRAS NEGRAS, PIEDRAS NEGRAS, COAHUILA.", "0", null },
                    { "280", "28", true, "PROGRESO, PROGRESO, YUCATÁN.", "0", null },
                    { "300", "30", true, "CIUDAD REYNOSA, REYNOSA, TAMAULIPAS.", "0", null },
                    { "310", "31", true, "SALINA CRUZ, SALINA CRUZ, OAXACA.", "0", null },
                    { "330", "33", true, "SAN LUIS RÍO COLORADO, SAN LUIS RÍO COLORADO, SONORA.", "0", null },
                    { "340", "34", true, "CIUDAD MIGUEL ALEMÁN, MIGUEL ALEMÁN, TAMAULIPAS.", "0", null },
                    { "370", "37", true, "CIUDAD HIDALGO, SUCHIATE, CHIAPAS.", "0", null },
                    { "380", "38", true, "TAMPICO, TAMPICO, TAMAULIPAS.", "0", null },
                    { "390", "39", true, "TECATE, TECATE, BAJA CALIFORNIA.", "0", null },
                    { "400", "40", true, "TIJUANA, TIJUANA, BAJA CALIFORNIA.", "0", null },
                    { "420", "42", true, "TUXPAN, TUXPAN, VERACRUZ.", "0", null },
                    { "430", "43", true, "VERACRUZ, VERACRUZ, VERACRUZ.", "0", null },
                    { "440", "44", true, "CIUDAD ACUÑA, ACUÑA, COAHUILA.", "0", null },
                    { "460", "46", true, "TORREÓN, TORREÓN, COAHUILA.", "0", null },
                    { "470", "47", true, "AEROPUERTO INTERNACIONAL DE LA CIUDAD DE MÉXICO.", "0", null },
                    { "480", "48", true, "GUADALAJARA, TLAJOMULCO DE ZÚÑIGA, JALISCO.", "0", null },
                    { "500", "50", true, "SONOYTA, PLUTARCO ELÍAS CALLES, SONORA.", "0", null },
                    { "510", "51", true, "LÁZARO CÁRDENAS, LÁZARO CÁRDENAS, MICHOACÁN.", "0", null },
                    { "520", "52", true, "MONTERREY, GENERAL MARIANO ESCOBEDO, NUEVO LEÓN.", "0", null },
                    { "530", "53", true, "CANCÚN, BENITO JUÁREZ, QUINTANA ROO.", "0", null },
                    { "640", "64", true, "QUERÉTARO, COLÓN, QUERÉTARO.", "0", null },
                    { "650", "65", true, "TOLUCA, TOLUCA, ESTADO DE MÉXICO.", "0", null },
                    { "670", "67", true, "CHIHUAHUA, CHIHUAHUA, CHIHUAHUA.", "0", null },
                    { "730", "73", true, "AGUASCALIENTES, AGUASCALIENTES, AGUASCALIENTES.", "0", null },
                    { "750", "75", true, "PUEBLA, HUEJOTZINGO, PUEBLA.", "0", null },
                    { "800", "80", true, "COLOMBIA, ANÁHUAC, NUEVO LEÓN.", "0", null },
                    { "810", "81", true, "ALTAMIRA, ALTAMIRA, TAMAULIPAS.", "0", null },
                    { "820", "82", true, "CIUDAD CAMARGO, CAMARGO, TAMAULIPAS.", "0", null },
                    { "830", "83", true, "DOS BOCAS, PARAÍSO, TABASCO.", "0", null },
                    { "840", "84", true, "GUANAJUATO, SILAO DE LA VICTORIA, GUANAJUATO.", "0", null },
                    { "850", "85", true, "AEROPUERTO INTERNACIONAL FELIPE ÁNGELES, ZUMPANGO, ESTADO DE MÉXICO.", "0", null }
                });

            migrationBuilder.InsertData(
                schema: "sidapa",
                table: "cat_tipo_operacion",
                columns: new[] { "clave", "descripcion" },
                values: new object[,]
                {
                    { (short)1, "Importación" },
                    { (short)2, "Exportación" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "010");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "020");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "050");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "060");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "070");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "080");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "110");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "120");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "140");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "160");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "170");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "180");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "190");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "200");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "220");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "230");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "240");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "250");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "260");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "270");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "280");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "300");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "310");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "330");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "340");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "370");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "380");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "390");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "400");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "420");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "430");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "440");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "460");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "470");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "480");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "500");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "510");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "520");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "530");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "640");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "650");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "670");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "730");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "750");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "800");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "810");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "820");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "830");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "840");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_aduana_seccion",
                keyColumn: "clave",
                keyValue: "850");

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_operacion",
                keyColumn: "clave",
                keyValue: (short)1);

            migrationBuilder.DeleteData(
                schema: "sidapa",
                table: "cat_tipo_operacion",
                keyColumn: "clave",
                keyValue: (short)2);

            migrationBuilder.DropSequence(
                name: "seq_numero_referencia",
                schema: "sidapa");
        }
    }
}
