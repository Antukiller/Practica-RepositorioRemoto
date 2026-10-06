namespace Practicas_RepositorioRemoto.Notifications;

/// <summary>
/// Define el contrato del servicio de emisión y suscripción a notificaciones.
/// </summary>
public interface INotificationService {
    /// <summary>
    /// Obtiene el flujo observable al que pueden suscribirse los consumidores
    /// para recibir notificaciones.
    /// </summary>
    /// <value>Flujo de eventos de tipo <see cref="Notification"/>.</value>
    IObservable<Notification> Observable { get; }

    /// <summary>
    /// Emite una notificación a los suscriptores del servicio.
    /// </summary>
    /// <param name="notificacion">Notificación que se desea emitir.</param>
    void Notificar(Notification notificacion);

    /// <summary>
    /// Notifica la creación de un usuario.
    /// </summary>
    /// <param name="id">Identificador del usuario creado.</param>
    void NotificarCreado(int id);

    /// <summary>
    /// Notifica la actualización de un usuario.
    /// </summary>
    /// <param name="id">Identificador del usuario actualizado.</param>
    void NotificarActualizado(int id);

    /// <summary>
    /// Notifica la eliminación de un usuario.
    /// </summary>
    /// <param name="id">Identificador del usuario eliminado.</param>
    void NotificarEliminado(int id);
}