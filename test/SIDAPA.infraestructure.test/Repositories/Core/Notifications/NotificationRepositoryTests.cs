using Microsoft.EntityFrameworkCore;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.infraestructure.Persistence;
using SIDAPA.infraestructure.Repositories;
using SIDAPA.infraestructure.test.Persistence;

namespace SIDAPA.infraestructure.test.Repositories.Core.Notifications;

/// <summary>
/// <see cref="NotificationRepository"/> contra PostgreSQL real (SCRUM-59). Reemplaza la parte de base de datos
/// de la página manual /dev/avisos: lista reciente, no leídos y "marcar como leída" solo para el dueño.
/// </summary>
[Collection(PostgresCollection.Name)]
public class NotificationRepositoryTests(PostgresFixture db)
{
    private static readonly DateTime Base = new(2026, 11, 23, 10, 0, 0);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static async Task<NotificationEntity> AddAsync(SidapaDbContext context, int userId, string message,
        DateTime createdAt, bool isRead = false)
    {
        var notification = new NotificationEntity
        {
            UserId = userId,
            Message = message,
            IsRead = isRead,
            IsEmailSent = false,
            CreatedAt = createdAt
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        return notification;
    }

    [Fact]
    public async Task AddAsync_GuardaElAvisoYLeAsignaId()
    {
        await using var arrange = db.CreateContext();
        var user = await TestData.UserAsync(arrange);
        var notification = new NotificationEntity { UserId = user.Id, Message = "Hola", CreatedAt = Base };

        await using (var context = db.CreateContext())
            await new NotificationRepository(context).AddAsync(notification, _ct);

        notification.Id.ShouldBeGreaterThan(0);
        await using var assert = db.CreateContext();
        var saved = await assert.Notifications.SingleAsync(n => n.Id == notification.Id, _ct);
        saved.Message.ShouldBe("Hola");
        saved.IsRead.ShouldBeFalse();
        saved.CreatedAt.ShouldBe(Base);
    }

    [Fact]
    public async Task GetRecentAsync_SoloDelUsuario_MasRecientePrimero_YRespetaTake()
    {
        await using var arrange = db.CreateContext();
        var user = await TestData.UserAsync(arrange);
        var other = await TestData.UserAsync(arrange);
        await AddAsync(arrange, user.Id, "viejo", Base);
        await AddAsync(arrange, user.Id, "medio", Base.AddMinutes(1));
        await AddAsync(arrange, user.Id, "nuevo", Base.AddMinutes(2));
        await AddAsync(arrange, other.Id, "de otro", Base.AddMinutes(3));

        await using var context = db.CreateContext();
        var recent = await new NotificationRepository(context).GetRecentAsync(user.Id, take: 2, _ct);

        recent.Select(n => n.Message).ShouldBe(["nuevo", "medio"]);
    }

    [Fact]
    public async Task GetRecentAsync_MismaFecha_DesempataPorIdDescendente()
    {
        await using var arrange = db.CreateContext();
        var user = await TestData.UserAsync(arrange);
        var first = await AddAsync(arrange, user.Id, "primero", Base);
        var second = await AddAsync(arrange, user.Id, "segundo", Base);

        await using var context = db.CreateContext();
        var recent = await new NotificationRepository(context).GetRecentAsync(user.Id, take: 10, _ct);

        recent.Select(n => n.Id).ShouldBe([second.Id, first.Id]);
    }

    [Fact]
    public async Task CountUnreadAsync_CuentaSoloNoLeidosDelUsuario()
    {
        await using var arrange = db.CreateContext();
        var user = await TestData.UserAsync(arrange);
        var other = await TestData.UserAsync(arrange);
        await AddAsync(arrange, user.Id, "a", Base);
        await AddAsync(arrange, user.Id, "b", Base);
        await AddAsync(arrange, user.Id, "leído", Base, isRead: true);
        await AddAsync(arrange, other.Id, "de otro", Base);

        await using var context = db.CreateContext();
        var count = await new NotificationRepository(context).CountUnreadAsync(user.Id, _ct);

        count.ShouldBe(2);
    }

    [Fact]
    public async Task MarkAsReadAsync_DelDueno_LaMarcaYRegresaTrue()
    {
        await using var arrange = db.CreateContext();
        var user = await TestData.UserAsync(arrange);
        var notification = await AddAsync(arrange, user.Id, "a", Base);

        await using (var context = db.CreateContext())
            (await new NotificationRepository(context).MarkAsReadAsync(user.Id, notification.Id, _ct)).ShouldBeTrue();

        await using var assert = db.CreateContext();
        (await assert.Notifications.SingleAsync(n => n.Id == notification.Id, _ct)).IsRead.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsReadAsync_DeOtroUsuario_NoLaTocaYRegresaFalse()
    {
        await using var arrange = db.CreateContext();
        var owner = await TestData.UserAsync(arrange);
        var intruder = await TestData.UserAsync(arrange);
        var notification = await AddAsync(arrange, owner.Id, "a", Base);

        await using (var context = db.CreateContext())
            (await new NotificationRepository(context).MarkAsReadAsync(intruder.Id, notification.Id, _ct)).ShouldBeFalse();

        await using var assert = db.CreateContext();
        (await assert.Notifications.SingleAsync(n => n.Id == notification.Id, _ct)).IsRead.ShouldBeFalse();
    }

    [Fact]
    public async Task MarkAsReadAsync_Inexistente_RegresaFalse()
    {
        await using var arrange = db.CreateContext();
        var user = await TestData.UserAsync(arrange);

        await using var context = db.CreateContext();
        (await new NotificationRepository(context).MarkAsReadAsync(user.Id, notificationId: int.MaxValue, _ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task MarkEmailSentAsync_MarcaSoloEseAviso()
    {
        await using var arrange = db.CreateContext();
        var user = await TestData.UserAsync(arrange);
        var sent = await AddAsync(arrange, user.Id, "a", Base);
        var notSent = await AddAsync(arrange, user.Id, "b", Base);

        await using (var context = db.CreateContext())
            await new NotificationRepository(context).MarkEmailSentAsync(sent.Id, _ct);

        await using var assert = db.CreateContext();
        (await assert.Notifications.SingleAsync(n => n.Id == sent.Id, _ct)).IsEmailSent.ShouldBeTrue();
        (await assert.Notifications.SingleAsync(n => n.Id == notSent.Id, _ct)).IsEmailSent.ShouldBeFalse();
    }
}
