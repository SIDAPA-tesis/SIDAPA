using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIDAPA.domain.Entities.CustomDeclaration.Catalogs;

namespace SIDAPA.infraestructure.Persistence.Configurations.CustomDeclaration.Catalogs;

public class AduanaConfiguration : IEntityTypeConfiguration<AduanaEntity>
{
    public void Configure(EntityTypeBuilder<AduanaEntity> builder)
    {
        builder.ToTable("cat_aduana_seccion");
        builder.HasKey(x => x.Code).HasName("pk_cat_aduana_seccion");

        builder.Property(x => x.Code).HasColumnName("clave").HasMaxLength(3).IsFixedLength();
        builder.Property(x => x.AduanaCode).HasColumnName("clave_aduana").HasMaxLength(2).IsFixedLength();
        builder.Property(x => x.Section).HasColumnName("seccion").HasMaxLength(1).IsFixedLength();
        builder.Property(x => x.Name).HasColumnName("denominacion").HasMaxLength(200);
        builder.Property(x => x.IsActive).HasColumnName("vigente");
        builder.Property(x => x.UpdatedOn).HasColumnName("fecha_actualizacion");

        builder.HasData(Apendice1.Select(a => new AduanaEntity
        {
            Code = a.Aduana + "0",
            AduanaCode = a.Aduana,
            Section = "0",
            Name = a.Name,
            IsActive = true
        }));
    }

    // Apéndice 1 del Anexo 22: aduanas (sección 0).
    // TODO verificar contra DOF/Anexo 22 vigente: se capturó de memoria (SCRUM-46) y faltan las
    // secciones aduaneras (clave con sección distinta de 0).
    private static readonly (string Aduana, string Name)[] Apendice1 =
    [
        ("01", "ACAPULCO, ACAPULCO DE JUÁREZ, GUERRERO."),
        ("02", "AGUA PRIETA, AGUA PRIETA, SONORA."),
        ("05", "SUBTENIENTE LÓPEZ, OTHÓN P. BLANCO, QUINTANA ROO."),
        ("06", "CIUDAD DEL CARMEN, CARMEN, CAMPECHE."),
        ("07", "CIUDAD JUÁREZ, JUÁREZ, CHIHUAHUA."),
        ("08", "COATZACOALCOS, COATZACOALCOS, VERACRUZ."),
        ("11", "ENSENADA, ENSENADA, BAJA CALIFORNIA."),
        ("12", "GUAYMAS, GUAYMAS, SONORA."),
        ("14", "LA PAZ, LA PAZ, BAJA CALIFORNIA SUR."),
        ("16", "MANZANILLO, MANZANILLO, COLIMA."),
        ("17", "MATAMOROS, MATAMOROS, TAMAULIPAS."),
        ("18", "MAZATLÁN, MAZATLÁN, SINALOA."),
        ("19", "MEXICALI, MEXICALI, BAJA CALIFORNIA."),
        ("20", "MÉXICO, CIUDAD DE MÉXICO."),
        ("22", "NACO, NACO, SONORA."),
        ("23", "NOGALES, NOGALES, SONORA."),
        ("24", "NUEVO LAREDO, NUEVO LAREDO, TAMAULIPAS."),
        ("25", "OJINAGA, OJINAGA, CHIHUAHUA."),
        ("26", "PUERTO PALOMAS, ASCENSIÓN, CHIHUAHUA."),
        ("27", "PIEDRAS NEGRAS, PIEDRAS NEGRAS, COAHUILA."),
        ("28", "PROGRESO, PROGRESO, YUCATÁN."),
        ("30", "CIUDAD REYNOSA, REYNOSA, TAMAULIPAS."),
        ("31", "SALINA CRUZ, SALINA CRUZ, OAXACA."),
        ("33", "SAN LUIS RÍO COLORADO, SAN LUIS RÍO COLORADO, SONORA."),
        ("34", "CIUDAD MIGUEL ALEMÁN, MIGUEL ALEMÁN, TAMAULIPAS."),
        ("37", "CIUDAD HIDALGO, SUCHIATE, CHIAPAS."),
        ("38", "TAMPICO, TAMPICO, TAMAULIPAS."),
        ("39", "TECATE, TECATE, BAJA CALIFORNIA."),
        ("40", "TIJUANA, TIJUANA, BAJA CALIFORNIA."),
        ("42", "TUXPAN, TUXPAN, VERACRUZ."),
        ("43", "VERACRUZ, VERACRUZ, VERACRUZ."),
        ("44", "CIUDAD ACUÑA, ACUÑA, COAHUILA."),
        ("46", "TORREÓN, TORREÓN, COAHUILA."),
        ("47", "AEROPUERTO INTERNACIONAL DE LA CIUDAD DE MÉXICO."),
        ("48", "GUADALAJARA, TLAJOMULCO DE ZÚÑIGA, JALISCO."),
        ("50", "SONOYTA, PLUTARCO ELÍAS CALLES, SONORA."),
        ("51", "LÁZARO CÁRDENAS, LÁZARO CÁRDENAS, MICHOACÁN."),
        ("52", "MONTERREY, GENERAL MARIANO ESCOBEDO, NUEVO LEÓN."),
        ("53", "CANCÚN, BENITO JUÁREZ, QUINTANA ROO."),
        ("64", "QUERÉTARO, COLÓN, QUERÉTARO."),
        ("65", "TOLUCA, TOLUCA, ESTADO DE MÉXICO."),
        ("67", "CHIHUAHUA, CHIHUAHUA, CHIHUAHUA."),
        ("73", "AGUASCALIENTES, AGUASCALIENTES, AGUASCALIENTES."),
        ("75", "PUEBLA, HUEJOTZINGO, PUEBLA."),
        ("80", "COLOMBIA, ANÁHUAC, NUEVO LEÓN."),
        ("81", "ALTAMIRA, ALTAMIRA, TAMAULIPAS."),
        ("82", "CIUDAD CAMARGO, CAMARGO, TAMAULIPAS."),
        ("83", "DOS BOCAS, PARAÍSO, TABASCO."),
        ("84", "GUANAJUATO, SILAO DE LA VICTORIA, GUANAJUATO."),
        ("85", "AEROPUERTO INTERNACIONAL FELIPE ÁNGELES, ZUMPANGO, ESTADO DE MÉXICO.")
    ];
}
