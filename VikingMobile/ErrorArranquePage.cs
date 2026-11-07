using VikingMobile.Diagnostics;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace VikingMobile;

/// <summary>
/// Página construida a mano (sin XAML) a propósito: si el problema
/// está en el parseo de XAML de la app, una página que también
/// depende de XAML podría fallar igual. Esta es la última red de
/// seguridad para que el usuario vea ALGO en vez de una pantalla
/// negra o que la app se cierre sola.
/// </summary>
public class ErrorArranquePage : ContentPage
{
    public ErrorArranquePage(Exception ex)
    {
        BackgroundColor = Colors.Black;

        var titulo = new Label
        {
            Text = "Viking Mobile no pudo iniciar",
            TextColor = Colors.White,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 40, 0, 10)
        };

        var subtitulo = new Label
        {
            Text = "Copia este mensaje y compártelo para poder corregirlo:",
            TextColor = Colors.Gray,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 16)
        };

        var detalle = new Label
        {
            Text = ex.ToString(),
            TextColor = Colors.White,
            FontSize = 12,
            FontFamily = "Courier"
        };

        var boton = new Button
        {
            Text = "COPIAR ERROR",
            TextColor = Colors.Black,
            BackgroundColor = Colors.White,
            Margin = new Thickness(0, 20, 0, 0)
        };
        boton.Clicked += async (_, _) =>
        {
            await Clipboard.Default.SetTextAsync(ex.ToString());
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 20),
                Children = { titulo, subtitulo, detalle, boton }
            }
        };
    }
}
