using Microsoft.EntityFrameworkCore;
using SIDAPA.domain.Entities.Core.Catalogs;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Entities.CustomDeclaration.Catalogs;
using SIDAPA.domain.Entities.CustomDeclaration.Operative;

namespace SIDAPA.infraestructure.Persistence;

/// <summary>
/// Unit of work for the SIDAPA PostgreSQL database (schema <c>sidapa</c>).
/// Table/column mappings live in <c>Persistence/Configurations</c>, one file per entity.
/// </summary>
public class SidapaDbContext(DbContextOptions<SidapaDbContext> options) : DbContext(options)
{
    // Core / Catalogs
    public DbSet<SystemParameterEntity> SystemParameters => Set<SystemParameterEntity>();
    public DbSet<OperationTypeEntity> OperationTypes => Set<OperationTypeEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<StatusEntity> Statuses => Set<StatusEntity>();
    public DbSet<DocumentTypeEntity> DocumentTypes => Set<DocumentTypeEntity>();
    public DbSet<RequiredDocumentEntity> RequiredDocuments => Set<RequiredDocumentEntity>();

    // Core / Operative
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<SupplierEntity> Suppliers => Set<SupplierEntity>();
    public DbSet<ReferenciaEntity> Referencias => Set<ReferenciaEntity>();
    public DbSet<StatusHistoryEntity> StatusHistories => Set<StatusHistoryEntity>();
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<DocumentRequestEntity> DocumentRequests => Set<DocumentRequestEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    // CustomDeclaration / Catalogs (Anexo 22)
    public DbSet<AduanaEntity> Aduanas => Set<AduanaEntity>();
    public DbSet<PedimentoKeyEntity> PedimentoKeys => Set<PedimentoKeyEntity>();
    public DbSet<TransportModeEntity> TransportModes => Set<TransportModeEntity>();
    public DbSet<CountryEntity> Countries => Set<CountryEntity>();
    public DbSet<CurrencyEntity> Currencies => Set<CurrencyEntity>();
    public DbSet<CountryCurrencyEntity> CountryCurrencies => Set<CountryCurrencyEntity>();
    public DbSet<RecintoFiscalizadoEntity> RecintosFiscalizados => Set<RecintoFiscalizadoEntity>();
    public DbSet<UnitOfMeasureEntity> UnitsOfMeasure => Set<UnitOfMeasureEntity>();
    public DbSet<IdentificadorEntity> Identificadores => Set<IdentificadorEntity>();
    public DbSet<GovernmentAgencyEntity> GovernmentAgencies => Set<GovernmentAgencyEntity>();
    public DbSet<RegulationEntity> Regulations => Set<RegulationEntity>();
    public DbSet<ContainerTypeEntity> ContainerTypes => Set<ContainerTypeEntity>();
    public DbSet<ValuationMethodEntity> ValuationMethods => Set<ValuationMethodEntity>();
    public DbSet<TaxEntity> Taxes => Set<TaxEntity>();
    public DbSet<PaymentMethodEntity> PaymentMethods => Set<PaymentMethodEntity>();
    public DbSet<IncotermEntity> Incoterms => Set<IncotermEntity>();
    public DbSet<GoodsDestinationEntity> GoodsDestinations => Set<GoodsDestinationEntity>();
    public DbSet<CustomsRegimeEntity> CustomsRegimes => Set<CustomsRegimeEntity>();
    public DbSet<RateTypeEntity> RateTypes => Set<RateTypeEntity>();
    public DbSet<HazardousSubstanceClassEntity> HazardousSubstanceClasses => Set<HazardousSubstanceClassEntity>();
    public DbSet<HazardousSubstanceDivisionEntity> HazardousSubstanceDivisions => Set<HazardousSubstanceDivisionEntity>();
    public DbSet<RfeEntity> Rfes => Set<RfeEntity>();
    public DbSet<RfeOperatorEntity> RfeOperators => Set<RfeOperatorEntity>();
    public DbSet<FraccionArancelariaEntity> FraccionesArancelarias => Set<FraccionArancelariaEntity>();
    public DbSet<ExchangeRateEntity> ExchangeRates => Set<ExchangeRateEntity>();
    public DbSet<CustomsBrokerEntity> CustomsBrokers => Set<CustomsBrokerEntity>();
    public DbSet<PedimentoSequenceEntity> PedimentoSequences => Set<PedimentoSequenceEntity>();

    // CustomDeclaration / Operative
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceLineEntity> InvoiceLines => Set<InvoiceLineEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<PedimentoEntity> Pedimentos => Set<PedimentoEntity>();
    public DbSet<PedimentoTransportEntity> PedimentoTransports => Set<PedimentoTransportEntity>();
    public DbSet<WaybillEntity> Waybills => Set<WaybillEntity>();
    public DbSet<ContainerEntity> Containers => Set<ContainerEntity>();
    public DbSet<PedimentoIdentificadorEntity> PedimentoIdentificadores => Set<PedimentoIdentificadorEntity>();
    public DbSet<PedimentoTaxEntity> PedimentoTaxes => Set<PedimentoTaxEntity>();
    public DbSet<PartidaEntity> Partidas => Set<PartidaEntity>();
    public DbSet<PartidaTaxEntity> PartidaTaxes => Set<PartidaTaxEntity>();
    public DbSet<PartidaIdentificadorEntity> PartidaIdentificadores => Set<PartidaIdentificadorEntity>();
    public DbSet<PartidaPermitEntity> PartidaPermits => Set<PartidaPermitEntity>();
    public DbSet<AnnotationEntity> Annotations => Set<AnnotationEntity>();
    public DbSet<ValidationFileEntity> ValidationFiles => Set<ValidationFileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SidapaDbContext.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SidapaDbContext).Assembly);
    }

    public const string Schema = "sidapa";
}
