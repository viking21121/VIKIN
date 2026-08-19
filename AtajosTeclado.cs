using System;
using System.Windows.Forms;

namespace AsistenciaNatacion;

/// <summary>
/// Utilidades para el manejo centralizado de atajos del MainForm.
/// Los callbacks se suministran desde MainForm para no acoplar esta clase a formularios concretos.
/// </summary>
public static class AtajosTeclado
{
    public static bool Procesar(
        KeyEventArgs e,
        Action? abrirAsistencia,
        Action? abrirAbono,
        Action? nuevoAlumno,
        Action? refrescar,
        Action? enfocarBusqueda,
        Action? limpiarOCerrar)
    {
        if (e.Control && e.KeyCode == Keys.F)
        {
            enfocarBusqueda?.Invoke();
            e.SuppressKeyPress = true;
            e.Handled = true;
            return true;
        }

        switch (e.KeyCode)
        {
            case Keys.F2:
                abrirAsistencia?.Invoke();
                break;
            case Keys.F3:
                abrirAbono?.Invoke();
                break;
            case Keys.F4:
                nuevoAlumno?.Invoke();
                break;
            case Keys.F5:
                refrescar?.Invoke();
                break;
            case Keys.Escape:
                limpiarOCerrar?.Invoke();
                break;
            default:
                return false;
        }

        e.SuppressKeyPress = true;
        e.Handled = true;
        return true;
    }
}
