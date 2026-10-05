namespace Practicas_RepositorioRemoto.Errors;

/// <summary>
/// Error de validacion que acumula todos los fallos detectados en una entidad.
/// </summary>
/// <remarks>
/// Los validadores recorren todas las reglas y las acumulan aqui, de modo que el cliente
/// recibe la lista completa de errores en una sola respuesta en lugar de tener que
/// corregirlos de uno en uno.
/// </remarks>
/// <param name="Errors">Mensajes de todos los errores detectados, en orden de aparicion</param>
public sealed record Validation(IReadOnlyList<string> Errors) : DomainError;