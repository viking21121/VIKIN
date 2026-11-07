using System.Globalization;

namespace VikingMobile.Converters;

/// <summary>Traduce "activo"/"por_vencer"/"moroso" al color de fondo del badge.</summary>
public class EstadoMembresiaColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.Trim().ToLowerInvariant() switch
        {
            "por_vencer" => Color.FromArgb("#B45309"),
            "moroso" => Color.FromArgb("#B91C1C"),
            _ => Color.FromArgb("#16803A")
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Traduce "activo"/"por_vencer"/"moroso" al texto del badge.</summary>
public class EstadoMembresiaTextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.Trim().ToLowerInvariant() switch
        {
            "por_vencer" => "POR VENCER",
            "moroso" => "MOROSO",
            _ => "ACTIVO"
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>True cuando el valor decimal es mayor que cero (para IsVisible).</summary>
public class MayorQueCeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is decimal d && d > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Traduce si una solicitud de pago ya tiene comprobante subido, para mostrarlo en la lista de administrador.</summary>
public class ComprobanteEstadoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool tiene && tiene
            ? "📎 Comprobante subido — revisar"
            : "⏳ Esperando comprobante del alumno";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Color del badge de una solicitud de pago, vista por el alumno (Mi Cuenta).</summary>
public class SolicitudEstadoColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.Trim().ToLowerInvariant() switch
        {
            "comprobante_subido" => Color.FromArgb("#B45309"),
            "aprobado" => Color.FromArgb("#16803A"),
            "rechazado" => Color.FromArgb("#B91C1C"),
            _ => Color.FromArgb("#3B4252")
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Texto del badge de una solicitud de pago, vista por el alumno (Mi Cuenta).</summary>
public class SolicitudEstadoTextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.Trim().ToLowerInvariant() switch
        {
            "comprobante_subido" => "EN REVISIÓN",
            "aprobado" => "APROBADO",
            "rechazado" => "RECHAZADO — VUELVE A SUBIRLO",
            _ => "PENDIENTE DE PAGO"
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>True cuando el estado de la solicitud es "rechazado" (para mostrar el comentario del administrador).</summary>
public class EsRechazadoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value as string, "rechazado", StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>True cuando el alumno todavía puede subir (o volver a subir) el comprobante.</summary>
public class PuedeSubirComprobanteConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.Trim().ToLowerInvariant() is "pendiente" or "comprobante_subido" or "rechazado";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
