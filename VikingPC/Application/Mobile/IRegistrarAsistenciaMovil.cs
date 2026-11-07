namespace AsistenciaNatacion.Application.Mobile
{
    public interface IRegistrarAsistenciaMovil
    {
        MobileAttendanceResponse Ejecutar(MobileAttendanceRequest request);
    }
}
