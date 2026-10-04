using System.Text.RegularExpressions;

namespace Practicas_RepositorioRemoto.Validators;

/// <summary>
///     Reglas de validación reutilizables por todos los validadores del dominio.
/// </summary>
/// <remarks>
///     No se imponen longitudes mínimas ni máximas: el requisito de dominio es únicamente
///     que el texto exista y no esté en blanco. El formato se controla con expresiones regulares.
///     <para>
///         Los patrones se compilan una sola vez en campos estáticos y se ejecutan con
///         <see cref="RegexOptions.NonBacktracking" /> y timeout, para evitar Backtracking
///         Denegado de Servicio (ReDoS) con entradas proporcionadas por el usuario.
///     </para>
/// </remarks>
public static class CommonValidationExtensions {
    /// <summary>Opciones de ejecución comunes: sin distinción de mayúsculas, invariante cultural y sin backtracking.</summary>
    private const RegexOptions Opciones = RegexOptions.IgnoreCase
                                          | RegexOptions.CultureInvariant
                                          | RegexOptions.NonBacktracking;

    /// <summary>
    ///     Partes locales admitidas: letras, dígitos y símbolos válidos, separados por puntos simples.
    /// </summary>
    private const string Local = @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*";

    /// <summary>Etiquetas de dominio: alfanuméricos, sin guiones al inicio ni al final.</summary>
    private const string Dominio = @"[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?";

    /// <summary>Margen de tiempo máximo para evaluar cualquier patrón.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(100);

    /// <summary>Patrón de email: parte local, arroba y dominio con TLD alfabético de dos o más caracteres.</summary>
    private static readonly Regex RegexEmail = new(
        $@"^\s*{Local}@{Dominio}(?:\.{Dominio})*\.[a-z]{{2,}}\s*$", Opciones, Timeout);

    /// <summary>Patrón de teléfono español: prefijo +34 opcional, 9 cifras empezando por 6, 7, 8 o 9.</summary>
    private static readonly Regex RegexTelefono = new(
        @"^\s*(\+34[\s.\-]?)?[6-9]\d{2}[\s.\-]?\d{2}[\s.\-]?\d{3}\s*$", Opciones, Timeout);

    /// <summary>Patrón de URL http/https, con esquema opcional (JSONPlaceholder no lo devuelve).</summary>
    private static readonly Regex RegexUrl = new(
        @"^\s*https?://[a-z0-9\-._~:/?#\[\]@!$&'()*+,;=]+\.[a-z]{2,}(:\d+)?\S*\s*$", Opciones, Timeout);

    /// <summary>Patrón de coordenada decimal en formato texto ("40.0347" o "-70.7625").</summary>
    private static readonly Regex RegexCoordenada = new(
        @"^\s*-?\d{1,3}(\.\d+)?\s*$", Opciones, Timeout);

    /// <summary>Patrón de código postal: 5 dígitos con extensión opcional de 4 (formato USA de JSONPlaceholder).</summary>
    private static readonly Regex RegexCodigoPostal = new(
        @"^\s*\d{5}(-\d{4})?\s*$", Opciones, Timeout);

    /// <summary>Patrón de alias de usuario: solo caracteres alfanuméricos, '_' o '.'.</summary>
    private static readonly Regex RegexAlias = new(
        @"^\s*[a-z0-9._]+\s*$", Opciones, Timeout);

    /// <summary>
    ///     Valida que un texto sea obligatorio: no nulo, no vacío y sin ser solo espacios.
    /// </summary>
    public static bool IsNotBlank(this string? texto) {
        return !string.IsNullOrWhiteSpace(texto);
    }

    /// <summary>
    ///     Valida el formato de un email: parte local, arroba, dominio y TLD alfabético de mínimo dos caracteres.
    /// </summary>
    /// <remarks>
    ///     Admite símbolos válidos en la parte local y puntos simples, pero rechaza puntos al principio
    ///     o al final y dominios sin terminación.
    ///     Ejemplos válidos: "bret@srav.com", "jorge.o.smith@correo.es", "user+tag@sub.dominio.com".
    /// </remarks>
    public static bool IsValidEmail(this string email) {
        if (email.IsNotBlank() is false) return false;

        return RegexEmail.IsMatch(email);
    }

    /// <summary>
    ///     Valida un teléfono con formato español: prefijo +34 opcional, 9 dígitos que empiezan por 6, 7, 8 o 9,
    ///     separados por espacios, guiones o puntos de forma opcional.
    /// </summary>
    /// <remarks>
    ///     Ejemplos válidos: "600123456", "+34 600 12 34 56", "600-12-34-56".
    /// </remarks>
    public static bool IsValidSpanishPhone(this string telefono) {
        if (telefono.IsNotBlank() is false) return false;

        return RegexTelefono.IsMatch(telefono);
    }

    /// <summary>
    ///     Valida el formato de una URL http/https.
    /// </summary>
    /// <remarks>
    ///     JSONPlaceholder devuelve websites sin esquema (p. ej. "hildegard.org"), por lo que
    ///     se normaliza a "https://" antes de validarla.
    /// </remarks>
    public static bool IsValidWebsite(this string website) {
        if (website.IsNotBlank() is false) return false;

        var valor = website.Trim();

        if (!valor.Contains("://")) valor = $"https://{valor}";

        return RegexUrl.IsMatch(valor);
    }

    /// <summary>Valida una coordenada decimal en formato texto ("40.0347" o "-70.7625").</summary>
    public static bool IsValidCoordinate(this string coordenada) {
        if (coordenada.IsNotBlank() is false) return false;

        return RegexCoordenada.IsMatch(coordenada);
    }

    /// <summary>Valida un código postal de 5 dígitos, con extensión opcional de 4 (formato USA de JSONPlaceholder).</summary>
    public static bool IsValidZipCode(this string zipCode) {
        if (zipCode.IsNotBlank() is false) return false;

        return RegexCodigoPostal.IsMatch(zipCode);
    }

    /// <summary>Valida un alias de usuario: solo caracteres alfanuméricos, '_' o '.'.</summary>
    public static bool IsValidUserNameFormat(this string userName) {
        if (userName.IsNotBlank() is false) return false;

        return RegexAlias.IsMatch(userName);
    }
}