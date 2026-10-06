namespace Practicas_RepositorioRemoto.Enum;

/// <summary>
/// Tipo de operación de usuario que ha.originado una notificación.
/// </summary>
public enum TypeNotification {
    /// <summary>Se ha creado un usuario.</summary>
    Creado,

    /// <summary>Se ha actualizado un usuario.</summary>
    Actualizado,

    /// <summary>Se ha eliminado un usuario.</summary>
    Eliminado
}