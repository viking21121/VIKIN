namespace VikingMobile;

public partial class PoliticaPrivacidadPage : ContentPage
{
    public PoliticaPrivacidadPage()
    {
        InitializeComponent();
    }

    private async void VolverLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
