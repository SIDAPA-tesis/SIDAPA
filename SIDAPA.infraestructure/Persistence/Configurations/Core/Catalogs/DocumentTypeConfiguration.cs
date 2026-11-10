using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIDAPA.domain.Entities.Core.Catalogs;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.infraestructure.Persistence.Configurations.Core.Catalogs;

public class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentTypeEntity>
{
    public void Configure(EntityTypeBuilder<DocumentTypeEntity> builder)
    {
        builder.ToTable("cat_tipo_documento");
        builder.HasKey(x => x.Id).HasName("pk_cat_tipo_documento");

        builder.Property(x => x.Id).HasColumnName("id_tipo_documento").ValueGeneratedNever();
        builder.Property(x => x.Name).HasColumnName("nombre").HasMaxLength(150);

        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("uq_cat_tipo_documento_1");

        builder.HasData(
            DocumentType(DocumentTypeEnum.CONSTANCIA_SITUACION_FISCAL,
                "Constancia de Situación Fiscal (RFC activo)"),
            DocumentType(DocumentTypeEnum.OPINION_CUMPLIMIENTO_OBLIGACIONES_FISCALES,
                "Opinión de Cumplimiento de Obligaciones Fiscales (32-D positiva)"),
            DocumentType(DocumentTypeEnum.ACTA_CONSTITUTIVA_PODER_NOTARIAL,
                "Acta Constitutiva y Poder Notarial del Representante Legal"),
            DocumentType(DocumentTypeEnum.IDENTIFICACION_OFICIAL_REPRESENTANTE_LEGAL,
                "Identificación Oficial del Representante Legal"),
            DocumentType(DocumentTypeEnum.COMPROBANTE_DOMICILIO_FISCAL,
                "Comprobante de Domicilio Fiscal"),
            DocumentType(DocumentTypeEnum.ENCARGO_CONFERIDO_SAT,
                "Encargo Conferido ante el SAT"),
            DocumentType(DocumentTypeEnum.PADRON_IMPORTADORES_SECTORES_ESPECIFICOS,
                "Registro en el Padrón de Importadores / Padrón de Sectores Específicos (solo importación)"),
            DocumentType(DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR,
                "Factura Comercial / CFDI con Complemento de Comercio Exterior"),
            DocumentType(DocumentTypeEnum.LISTA_EMPAQUE,
                "Lista de Empaque (Packing List)"),
            DocumentType(DocumentTypeEnum.DOCUMENTO_TRANSPORTE,
                "Documento de Transporte (B/L, Guía Aérea o Carta Porte)"),
            DocumentType(DocumentTypeEnum.CERTIFICADO_PRUEBA_ORIGEN,
                "Certificado o Prueba de Origen"),
            DocumentType(DocumentTypeEnum.FICHA_TECNICA_HOJA_SEGURIDAD,
                "Ficha Técnica / Catálogo / Hoja de Seguridad (MSDS)"),
            DocumentType(DocumentTypeEnum.PERMISOS_LICENCIAS_CERTIFICADOS_RRNA_NOM,
                "Permisos, Licencias y Certificados de RRNAs / NOMs"),
            DocumentType(DocumentTypeEnum.POLIZA_SEGURO_TRANSPORTE,
                "Póliza o Certificado de Seguro de Transporte"),
            DocumentType(DocumentTypeEnum.MANIFESTACION_VALOR_HOJA_CALCULO,
                "Manifestación de Valor y Hoja de Cálculo (solo importación)"),
            DocumentType(DocumentTypeEnum.COMPROBANTES_PAGO_FLETES_GASTOS_INCREMENTABLES,
                "Comprobantes de Pago de Fletes y Gastos Incrementable"));
    }

    private static DocumentTypeEntity DocumentType(DocumentTypeEnum type, string name) =>
        new() { Id = (short)type, Name = name };
}
