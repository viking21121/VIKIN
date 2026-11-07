using System.Text.Json;
using VikingMobile.Application.Communication;

namespace VikingMobile.Application.Activities;

public class ActividadService
{
    private const string ActividadesCacheKey =
        "viking_actividades_cache";

    private readonly ICommunicationChannel _communicationChannel;

    public ActividadService(
        ICommunicationChannel communicationChannel)
    {
        _communicationChannel = communicationChannel;
    }

    public async Task<List<ActividadResponse>>
        ObtenerActividadesAsync()
    {
        try
        {
            string response =
                await _communicationChannel.GetAsync(
                    "api/actividades");

            var actividades =
                JsonSerializer.Deserialize<
                    List<ActividadResponse>>(
                        response,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

            var resultado =
                actividades ?? new List<ActividadResponse>();

            if (resultado.Count > 0)
            {
                GuardarActividadesLocalmente(resultado);
            }

            return resultado;
        }
        catch
        {
            return ObtenerActividadesLocales();
        }
    }

    public List<ActividadResponse>
        ObtenerActividadesLocales()
    {
        try
        {
            string json =
                Preferences.Get(
                    ActividadesCacheKey,
                    "");

            if (string.IsNullOrWhiteSpace(json))
                return new List<ActividadResponse>();

            return JsonSerializer.Deserialize<
                       List<ActividadResponse>>(
                       json,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       })
                   ?? new List<ActividadResponse>();
        }
        catch
        {
            return new List<ActividadResponse>();
        }
    }

    private void GuardarActividadesLocalmente(
        List<ActividadResponse> actividades)
    {
        try
        {
            string json =
                JsonSerializer.Serialize(
                    actividades);

            Preferences.Set(
                ActividadesCacheKey,
                json);
        }
        catch
        {
            // La caché no debe impedir el funcionamiento
            // normal de la aplicación.
        }
    }
}