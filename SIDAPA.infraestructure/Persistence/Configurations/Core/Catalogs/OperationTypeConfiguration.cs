using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIDAPA.domain.Entities.Core.Catalogs;
using SIDAPA.domain.Enums.Core;

namespace SIDAPA.infraestructure.Persistence.Configurations.Core.Catalogs;

public class OperationTypeConfiguration : IEntityTypeConfiguration<OperationTypeEntity>
{
    public void Configure(EntityTypeBuilder<OperationTypeEntity> builder)
    {
        builder.ToTable("cat_tipo_operacion");
        builder.HasKey(x => x.Code).HasName("pk_cat_tipo_operacion");

        builder.Property(x => x.Code).HasColumnName("clave").ValueGeneratedNever();
        builder.Property(x => x.Description).HasColumnName("descripcion").HasMaxLength(20);

        builder.HasData(
            new OperationTypeEntity { Code = (short)OperationTypeEnum.IMPORTACION, Description = "Importación" },
            new OperationTypeEntity { Code = (short)OperationTypeEnum.EXPORTACION, Description = "Exportación" });
    }
}
