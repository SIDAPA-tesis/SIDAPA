using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIDAPA.domain.Entities.Core.Catalogs;
using SIDAPA.domain.Entities.CustomDeclaration.Catalogs;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.infraestructure.Persistence.Configurations.Core.Catalogs;

public class RequiredDocumentConfiguration : IEntityTypeConfiguration<RequiredDocumentEntity>
{
    public void Configure(EntityTypeBuilder<RequiredDocumentEntity> builder)
    {
        builder.ToTable("cat_documento_requerido");
        builder.HasKey(x => x.Id).HasName("pk_cat_documento_requerido");

        builder.Property(x => x.Id).HasColumnName("id_documento_requerido").UseIdentityAlwaysColumn();
        builder.Property(x => x.OperationTypeCode).HasColumnName("tipo_operacion");
        builder.Property(x => x.PedimentoKeyCode).HasColumnName("clave_pedimento").HasMaxLength(2).IsFixedLength();
        builder.Property(x => x.DocumentTypeId).HasColumnName("id_tipo_documento");
        builder.Property(x => x.IsMandatory).HasColumnName("obligatorio");

        builder.HasOne(x => x.OperationType)
            .WithMany()
            .HasForeignKey(x => x.OperationTypeCode)
            .HasConstraintName("fk_cat_documento_requerido_tipo_operacion")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PedimentoKey)
            .WithMany()
            .HasForeignKey(x => x.PedimentoKeyCode)
            .HasConstraintName("fk_cat_documento_requerido_clave_pedimento")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DocumentType)
            .WithMany()
            .HasForeignKey(x => x.DocumentTypeId)
            .HasConstraintName("fk_cat_documento_requerido_id_tipo_documento")
            .OnDelete(DeleteBehavior.Restrict);

        // HU-03: lista de verificación por tipo de operación, para cualquier clave de pedimento (acordado con el
        // equipo, 2026-11-26). Los documentos 1–6 son del expediente del cliente, no de cada operación. Exportación no
        // lleva Padrón de importadores ni Manifestación de valor: el catálogo los marca "solo importación".
        builder.HasData(
            Import(1, DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, mandatory: true),
            Import(2, DocumentTypeEnum.LISTA_EMPAQUE, mandatory: true),
            Import(3, DocumentTypeEnum.DOCUMENTO_TRANSPORTE, mandatory: true),
            Import(4, DocumentTypeEnum.PADRON_IMPORTADORES_SECTORES_ESPECIFICOS, mandatory: false),
            Import(5, DocumentTypeEnum.CERTIFICADO_PRUEBA_ORIGEN, mandatory: false),
            Import(6, DocumentTypeEnum.FICHA_TECNICA_HOJA_SEGURIDAD, mandatory: false),
            Import(7, DocumentTypeEnum.PERMISOS_LICENCIAS_CERTIFICADOS_RRNA_NOM, mandatory: false),
            Import(8, DocumentTypeEnum.POLIZA_SEGURO_TRANSPORTE, mandatory: false),
            Import(9, DocumentTypeEnum.MANIFESTACION_VALOR_HOJA_CALCULO, mandatory: false),
            Import(10, DocumentTypeEnum.COMPROBANTES_PAGO_FLETES_GASTOS_INCREMENTABLES, mandatory: false),
            Export(11, DocumentTypeEnum.FACTURA_COMERCIAL_CFDI_COMERCIO_EXTERIOR, mandatory: true),
            Export(12, DocumentTypeEnum.LISTA_EMPAQUE, mandatory: true),
            Export(13, DocumentTypeEnum.DOCUMENTO_TRANSPORTE, mandatory: true),
            Export(14, DocumentTypeEnum.CERTIFICADO_PRUEBA_ORIGEN, mandatory: false),
            Export(15, DocumentTypeEnum.FICHA_TECNICA_HOJA_SEGURIDAD, mandatory: false),
            Export(16, DocumentTypeEnum.PERMISOS_LICENCIAS_CERTIFICADOS_RRNA_NOM, mandatory: false),
            Export(17, DocumentTypeEnum.POLIZA_SEGURO_TRANSPORTE, mandatory: false),
            Export(18, DocumentTypeEnum.COMPROBANTES_PAGO_FLETES_GASTOS_INCREMENTABLES, mandatory: false));
    }

    private static RequiredDocumentEntity Import(int id, DocumentTypeEnum type, bool mandatory) =>
        Required(id, OperationTypeEnum.IMPORTACION, type, mandatory);

    private static RequiredDocumentEntity Export(int id, DocumentTypeEnum type, bool mandatory) =>
        Required(id, OperationTypeEnum.EXPORTACION, type, mandatory);

    private static RequiredDocumentEntity Required(int id, OperationTypeEnum operation, DocumentTypeEnum type, bool mandatory) => new()
    {
        Id = id,
        OperationTypeCode = (short)operation,
        DocumentTypeId = (short)type,
        IsMandatory = mandatory
    };
}
