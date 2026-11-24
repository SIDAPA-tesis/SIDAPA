using NSubstitute.ExceptionExtensions;
using SIDAPA.application.DTOs.Core.Notifications;
using SIDAPA.application.Interface.Core;
using SIDAPA.application.Services.Core;
using SIDAPA.application.test.Fakes;
using SIDAPA.domain.Entities.Core.Operative;
using SIDAPA.domain.Exceptions.CoreException;

namespace SIDAPA.application.test.Services.Core.Notifications;

/// <summary>
/// Pruebas de <see cref="NotificationService"/> (SCRUM-59 · HU-04-T02): avisos en el panel y por correo.
/// Sin BD ni SMTP: los avisos viven en <see cref="FakeNotificationRepository"/> y el correo es un substitute.
/// </summary>
public class NotificationServiceTests
{
    private const int UserId = 9;
    private const int ClientId = 4;
    private const int ReferenciaId = 7;
    private const string Message = "Faltan documentos de la referencia R-000007.";

    private readonly FakeNotificationRepository _notifications = new();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly FixedTimeProvider _clock = new();
    private readonly NotificationService _service;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public NotificationServiceTests()
    {
        // Por omisión el correo sale bien; cada prueba de fallo lo sobreescribe.
        _email.SendAsync(Arg.Any<EmailMessageDto>(), Arg.Any<CancellationToken>()).Returns(true);
        _service = new NotificationService(_notifications, _users, _email, _clock);
    }

    private static UserEntity User(int id, string name = "Ana Pérez", string? email = null) =>
        new() { Id = id, Name = name, Email = email ?? $"usuario{id}@cliente.test", ClientId = ClientId, IsActive = true };

    private void GivenUser(UserEntity user) =>
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

    private void GivenClientUsers(params UserEntity[] users) =>
        _users.GetActiveByClientAsync(ClientId, Arg.Any<CancellationToken>()).Returns(users);

    private void EmailFailsFor(string to) =>
        _email.SendAsync(Arg.Is<EmailMessageDto>(m => m.To == to), Arg.Any<CancellationToken>()).Returns(false);

    private Task NoEmailWasSent() =>
        _email.DidNotReceiveWithAnyArgs().SendAsync(default!, default);

    // ------------------------------------------------------------------ NotifyUserAsync

    [Fact]
    public async Task NotifyUserAsync_ConCorreo_GuardaAvisoNoLeidoYLoMarcaEnviado()
    {
        GivenUser(User(UserId));

        await _service.NotifyUserAsync(UserId, Message, ReferenciaId, ct: _ct);

        var saved = _notifications.Saved.ShouldHaveSingleItem();
        saved.UserId.ShouldBe(UserId);
        saved.ReferenciaId.ShouldBe(ReferenciaId);
        saved.Message.ShouldBe(Message);
        saved.IsRead.ShouldBeFalse();
        saved.IsEmailSent.ShouldBeTrue();
    }

    [Fact]
    public async Task NotifyUserAsync_ConCorreo_MandaElCorreoAlUsuarioConSuNombreYElTexto()
    {
        GivenUser(User(UserId, name: "Ana Pérez", email: "ana@cliente.test"));

        await _service.NotifyUserAsync(UserId, Message, ct: _ct);

        await _email.Received(1).SendAsync(
            Arg.Is<EmailMessageDto>(m =>
                m.To == "ana@cliente.test" &&
                m.Subject == "SIDAPA · Tienes un nuevo aviso" &&
                m.Body.Contains("Hola Ana Pérez:") &&
                m.Body.Contains(Message)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyUserAsync_MensajeConEspacios_GuardaYMandaElTextoRecortado()
    {
        GivenUser(User(UserId));

        await _service.NotifyUserAsync(UserId, $"   {Message}   ", ct: _ct);

        _notifications.Saved.ShouldHaveSingleItem().Message.ShouldBe(Message);
        await _email.Received(1).SendAsync(
            Arg.Is<EmailMessageDto>(m => m.Body.Contains(Message) && !m.Body.Contains($" {Message}")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyUserAsync_SinReferencia_GuardaElAvisoSinReferencia()
    {
        GivenUser(User(UserId));

        await _service.NotifyUserAsync(UserId, Message, ct: _ct);

        _notifications.Saved.ShouldHaveSingleItem().ReferenciaId.ShouldBeNull();
    }

    [Fact]
    public async Task NotifyUserAsync_SinCorreo_GuardaElAvisoYNoMandaCorreo()
    {
        GivenUser(User(UserId));

        await _service.NotifyUserAsync(UserId, Message, sendEmail: false, ct: _ct);

        _notifications.Saved.ShouldHaveSingleItem().IsEmailSent.ShouldBeFalse();
        await NoEmailWasSent();
    }

    [Fact]
    public async Task NotifyUserAsync_ElCorreoNoSale_ElAvisoQuedaGuardadoSinMarcarEnviado()
    {
        var user = User(UserId);
        GivenUser(user);
        EmailFailsFor(user.Email);

        await _service.NotifyUserAsync(UserId, Message, ct: _ct);

        // docs/avisos.md: "si el correo falla, el aviso del panel se guarda igual (enviada_correo = false)".
        var saved = _notifications.Saved.ShouldHaveSingleItem();
        saved.IsEmailSent.ShouldBeFalse();
        saved.IsRead.ShouldBeFalse();
    }

    [Fact]
    public async Task NotifyUserAsync_UsuarioNoExiste_LanzaSinGuardarNiMandarNada()
    {
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((UserEntity?)null);

        await Should.ThrowAsync<UserNotFoundException>(() => _service.NotifyUserAsync(UserId, Message, ct: _ct));

        _notifications.Saved.ShouldBeEmpty();
        await NoEmailWasSent();
    }

    // IEmailSender no promete no lanzar: SmtpEmailSender atrapa sus errores, pero otro adaptador (o un
    // error antes del try, p. ej. una dirección mal formada) puede lanzar. Debe tratarse igual que "no salió".
    [Fact]
    public async Task NotifyUserAsync_ElCorreoLanzaExcepcion_ElAvisoQuedaGuardadoSinMarcarEnviadoYNoLanza()
    {
        GivenUser(User(UserId));
        _email.SendAsync(Arg.Any<EmailMessageDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP caído"));

        await _service.NotifyUserAsync(UserId, Message, ReferenciaId, ct: _ct);

        _notifications.Saved.ShouldHaveSingleItem().IsEmailSent.ShouldBeFalse();
    }

    // La cancelación no es "el correo falló": se respeta y sube al que llamó.
    [Fact]
    public async Task NotifyUserAsync_SeCancelaMientrasSaleElCorreo_PropagaLaCancelacion()
    {
        GivenUser(User(UserId));
        _email.SendAsync(Arg.Any<EmailMessageDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _service.NotifyUserAsync(UserId, Message, ct: _ct));

        _notifications.Saved.ShouldHaveSingleItem().IsEmailSent.ShouldBeFalse();
    }

    [Fact]
    public async Task NotifyUserAsync_GuardaLaHoraDelRelojEnUtcSinZona()
    {
        GivenUser(User(UserId));

        await _service.NotifyUserAsync(UserId, Message, ct: _ct);

        var createdAt = _notifications.Saved.ShouldHaveSingleItem().CreatedAt;
        createdAt.ShouldBe(new DateTime(2026, 11, 23, 15, 30, 0)); // FixedTimeProvider.Default
        createdAt.Kind.ShouldBe(DateTimeKind.Unspecified);         // columna "timestamp without time zone"
    }

    // Un reloj con offset de México (-06:00) debe guardar la hora UTC, no la local.
    [Fact]
    public async Task NotifyUserAsync_RelojConOffsetDeMexico_GuardaLaHoraEnUtc()
    {
        var mexico = new FixedTimeProvider(new DateTimeOffset(2026, 11, 23, 9, 30, 0, TimeSpan.FromHours(-6)));
        var service = new NotificationService(_notifications, _users, _email, mexico);
        GivenUser(User(UserId));

        await service.NotifyUserAsync(UserId, Message, ct: _ct);

        _notifications.Saved.ShouldHaveSingleItem().CreatedAt.ShouldBe(new DateTime(2026, 11, 23, 15, 30, 0));
    }

    // Los límites del mensaje (500 / 501, null, espacios) se prueban en NotificationRuleTests;
    // aquí solo importa que se valide ANTES de tocar usuarios, avisos o correo.
    [Fact]
    public async Task NotifyUserAsync_MensajeVacio_LanzaSinBuscarUsuarioNiGuardar()
    {
        GivenUser(User(UserId));

        await Should.ThrowAsync<EmptyNotificationMessageException>(() => _service.NotifyUserAsync(UserId, "   ", ct: _ct));

        await _users.DidNotReceive().GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _notifications.Saved.ShouldBeEmpty();
        await NoEmailWasSent();
    }

    // ------------------------------------------------------------------ NotifyClientAsync

    [Fact]
    public async Task NotifyClientAsync_TresUsuariosActivos_CadaUnoRecibeSuAvisoYSuCorreo()
    {
        GivenClientUsers(User(11, "Ana"), User(12, "Beto"), User(13, "Carla"));

        await _service.NotifyClientAsync(ClientId, Message, ReferenciaId, ct: _ct);

        _notifications.Saved.Select(n => n.UserId).ShouldBe([11, 12, 13], ignoreOrder: true);
        _notifications.Saved.ShouldAllBe(n =>
            n.Message == Message && n.ReferenciaId == ReferenciaId && !n.IsRead && n.IsEmailSent);

        // Un correo por persona, a su dirección y con su nombre (no un correo masivo con copia a todos).
        await _email.Received(3).SendAsync(Arg.Any<EmailMessageDto>(), Arg.Any<CancellationToken>());
        await _email.Received(1).SendAsync(Arg.Is<EmailMessageDto>(m => m.To == "usuario11@cliente.test" && m.Body.Contains("Hola Ana:")), Arg.Any<CancellationToken>());
        await _email.Received(1).SendAsync(Arg.Is<EmailMessageDto>(m => m.To == "usuario12@cliente.test" && m.Body.Contains("Hola Beto:")), Arg.Any<CancellationToken>());
        await _email.Received(1).SendAsync(Arg.Is<EmailMessageDto>(m => m.To == "usuario13@cliente.test" && m.Body.Contains("Hola Carla:")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyClientAsync_ClienteSinUsuariosActivos_NoGuardaNiMandaNadaYNoLanza()
    {
        GivenClientUsers();

        await _service.NotifyClientAsync(ClientId, Message, ReferenciaId, ct: _ct);

        _notifications.Saved.ShouldBeEmpty();
        await NoEmailWasSent();
    }

    [Fact]
    public async Task NotifyClientAsync_SinCorreo_TodosRecibenAvisoYNadieCorreo()
    {
        GivenClientUsers(User(11), User(12));

        await _service.NotifyClientAsync(ClientId, Message, sendEmail: false, ct: _ct);

        _notifications.Saved.Count.ShouldBe(2);
        _notifications.Saved.ShouldAllBe(n => !n.IsEmailSent);
        await NoEmailWasSent();
    }

    [Fact]
    public async Task NotifyClientAsync_ElCorreoDelPrimeroNoSale_LosDemasRecibenAvisoYCorreo()
    {
        var first = User(11);
        GivenClientUsers(first, User(12), User(13));
        EmailFailsFor(first.Email);

        await _service.NotifyClientAsync(ClientId, Message, ct: _ct);

        _notifications.Saved.Count.ShouldBe(3);
        _notifications.Saved.Single(n => n.UserId == 11).IsEmailSent.ShouldBeFalse();
        _notifications.Saved.Where(n => n.UserId != 11).ShouldAllBe(n => n.IsEmailSent);
        await _email.Received(3).SendAsync(Arg.Any<EmailMessageDto>(), Arg.Any<CancellationToken>());
    }

    // Riesgo principal: una excepción del correo del primer usuario NO debe dejar sin aviso a los demás.
    [Fact]
    public async Task NotifyClientAsync_ElCorreoDelPrimeroLanzaExcepcion_LosDemasRecibenAvisoYCorreo()
    {
        var first = User(11);
        GivenClientUsers(first, User(12), User(13));
        _email.SendAsync(Arg.Is<EmailMessageDto>(m => m.To == first.Email), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Buzón inexistente"));

        await _service.NotifyClientAsync(ClientId, Message, ReferenciaId, ct: _ct);

        _notifications.Saved.Select(n => n.UserId).ShouldBe([11, 12, 13], ignoreOrder: true);
        _notifications.Saved.Single(n => n.UserId == 11).IsEmailSent.ShouldBeFalse();
        _notifications.Saved.Where(n => n.UserId != 11).ShouldAllBe(n => n.IsEmailSent);
    }

    [Fact]
    public async Task NotifyClientAsync_TodosLosAvisosLlevanLaHoraDelReloj()
    {
        GivenClientUsers(User(11), User(12));

        await _service.NotifyClientAsync(ClientId, Message, ct: _ct);

        _notifications.Saved.ShouldAllBe(n => n.CreatedAt == new DateTime(2026, 11, 23, 15, 30, 0));
    }

    [Fact]
    public async Task NotifyClientAsync_MensajeDemasiadoLargo_LanzaSinConsultarUsuarios()
    {
        GivenClientUsers(User(11));

        await Should.ThrowAsync<NotificationMessageTooLongException>(() =>
            _service.NotifyClientAsync(ClientId, new string('x', 501), ct: _ct));

        await _users.DidNotReceive().GetActiveByClientAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _notifications.Saved.ShouldBeEmpty();
    }

    // Caracterización: avisar a un cliente NO es atómico. Si la BD falla con el segundo usuario, el
    // primero ya quedó avisado (y su correo ya salió), el tercero no, y el error sube al que llamó.
    [Fact]
    public async Task NotifyClientAsync_FallaLaBdConElSegundo_PropagaElErrorYElPrimeroYaQuedoAvisado()
    {
        GivenClientUsers(User(11), User(12), User(13));
        _notifications.FailOnAddForUserId = 12;

        await Should.ThrowAsync<InvalidOperationException>(() => _service.NotifyClientAsync(ClientId, Message, ct: _ct));

        _notifications.Saved.ShouldHaveSingleItem().UserId.ShouldBe(11);
        await _email.Received(1).SendAsync(Arg.Any<EmailMessageDto>(), Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ Panel

    [Fact]
    public async Task MarkAsReadAsync_AvisoPropio_LoMarcaLeido()
    {
        var own = _notifications.Seed(UserId);

        await _service.MarkAsReadAsync(UserId, own.Id, _ct);

        own.IsRead.ShouldBeTrue();
    }

    [Fact]
    public async Task MarkAsReadAsync_AvisoDeOtroUsuario_LanzaYNoLoMarca()
    {
        var other = _notifications.Seed(userId: 99);

        await Should.ThrowAsync<NotificationNotFoundException>(() => _service.MarkAsReadAsync(UserId, other.Id, _ct));

        other.IsRead.ShouldBeFalse();
    }

    [Fact]
    public async Task MarkAsReadAsync_AvisoQueNoExiste_Lanza()
    {
        await Should.ThrowAsync<NotificationNotFoundException>(() => _service.MarkAsReadAsync(UserId, 12345, _ct));
    }

    [Fact]
    public async Task GetRecentAsync_RegresaLosAvisosDelUsuarioComoDto()
    {
        var createdAt = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Unspecified);
        var mine = _notifications.Seed(UserId, Message, ReferenciaId, isRead: true, createdAt);
        _notifications.Seed(userId: 99);

        var result = await _service.GetRecentAsync(UserId, ct: _ct);

        result.ShouldHaveSingleItem().ShouldBe(new NotificationDto(mine.Id, Message, ReferenciaId, true, createdAt));
    }

    /// <summary>
    /// <c>ope_notificacion</c> en memoria. Asigna el Id al guardar (como la columna identity) y
    /// <see cref="MarkEmailSentAsync"/>/<see cref="MarkAsReadAsync"/> cambian el aviso guardado, para
    /// afirmar el estado final y no las llamadas.
    /// </summary>
    private sealed class FakeNotificationRepository : INotificationRepository
    {
        private readonly List<NotificationEntity> _rows = [];
        private int _nextId = 100;

        /// <summary>Avisos guardados por el servicio (sin los de <see cref="Seed"/>).</summary>
        public List<NotificationEntity> Saved { get; } = [];

        /// <summary>Si se fija, <see cref="AddAsync"/> lanza al guardar el aviso de ese usuario.</summary>
        public int? FailOnAddForUserId { get; set; }

        public NotificationEntity Seed(int userId, string message = "aviso previo", int? referenciaId = null,
            bool isRead = false, DateTime createdAt = default)
        {
            var row = new NotificationEntity
            {
                Id = _nextId++, UserId = userId, Message = message, ReferenciaId = referenciaId,
                IsRead = isRead, CreatedAt = createdAt
            };
            _rows.Add(row);
            return row;
        }

        public Task AddAsync(NotificationEntity notification, CancellationToken ct = default)
        {
            if (notification.UserId == FailOnAddForUserId)
                throw new InvalidOperationException("BD caída");

            notification.Id = _nextId++;
            _rows.Add(notification);
            Saved.Add(notification);
            return Task.CompletedTask;
        }

        public Task MarkEmailSentAsync(int notificationId, CancellationToken ct = default)
        {
            foreach (var row in _rows.Where(r => r.Id == notificationId))
                row.IsEmailSent = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NotificationEntity>> GetRecentAsync(int userId, int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<NotificationEntity>>(_rows.Where(r => r.UserId == userId).Take(take).ToList());

        public Task<int> CountUnreadAsync(int userId, CancellationToken ct = default) =>
            Task.FromResult(_rows.Count(r => r.UserId == userId && !r.IsRead));

        public Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default)
        {
            var row = _rows.FirstOrDefault(r => r.Id == notificationId && r.UserId == userId);
            if (row is null)
                return Task.FromResult(false);

            row.IsRead = true;
            return Task.FromResult(true);
        }
    }
}
