using Practicas_RepositorioRemoto.Enum;

namespace Practicas_RepositorioRemoto.Notifications;

/// <summary>
/// Representa una notificación sobre una operación realizada con un usuario.
/// </summary>
/// <param name="Tipo">Clase de operación que origina el evento.</param>
/// <param name="Mensaje">Descripción legible del evento.</param>
/// <param name="Timestamp">Momento en el que se produjo el evento.</param>
public record Notification(
    TypeNotification Tipo,
    string Mensaje,
    DateTime Timestamp
);