using System.Reactive.Linq;
using System.Reactive.Subjects;
using Practicas_RepositorioRemoto.Enum;
using Practicas_RepositorioRemoto.Interfaces;

namespace Practicas_RepositorioRemoto.Notifications;

/// <summary>
/// Servicio de notificaciones basado en <see cref="Subject{T}"/> de Rx.NET.
/// </summary>
/// <remarks>
/// <para>
/// Es un <see cref="ISingletonService"/> porque el <see cref="Subject{T}"/> debe ser
/// la misma instancia para todos: si fuera scoped o transient, las suscripciones de
/// <c>Program.cs</c> no recibirían las emisiones de las peticiones.
/// </para>
/// <para>
/// Se implementa como <see cref="IDisposable"/> para poder completar y liberar el flujo
/// cuando la aplicación se apaga.
/// </para>
/// </remarks>
public class ConsoleNotificationService : INotificationService, ISingletonService, IDisposable {
    /// <summary>
    /// Emisor de notificaciones que las distribuye a los suscriptores activos.
    /// </summary>
    /// <remarks>
    /// El tipo del <see cref="Subject{T}"/> debe ser el record <see cref="Notification"/> del
    /// dominio, no <c>System.Reactive.Notification</c> (que representa el error de un flujo).
    /// </remarks>
    private readonly Subject<Notification> _subject = new();

    /// <inheritdoc cref="INotificationService.Observable" />
    public IObservable<Notification> Observable => _subject.AsObservable();

    /// <inheritdoc cref="INotificationService.Notificar" />
    public void Notificar(Notification notificacion) => _subject.OnNext(notificacion);

    /// <inheritdoc cref="INotificationService.NotificarCreado" />
    public void NotificarCreado(int id) => Notificar(new Notification(
        TypeNotification.Creado, $"Usuario creado: {id}", DateTime.UtcNow));

    /// <inheritdoc cref="INotificationService.NotificarActualizado" />
    public void NotificarActualizado(int id) => Notificar(new Notification(
        TypeNotification.Actualizado, $"Usuario actualizado: {id}", DateTime.UtcNow));

    /// <inheritdoc cref="INotificationService.NotificarEliminado" />
    public void NotificarEliminado(int id) => Notificar(new Notification(
        TypeNotification.Eliminado, $"Usuario eliminado: {id}", DateTime.UtcNow));

    /// <summary>Completa el flujo y libera el <see cref="Subject{T}"/>.</summary>
    public void Dispose() {
        _subject.OnCompleted();
        _subject.Dispose();
    }
}