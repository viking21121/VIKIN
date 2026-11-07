using AsistenciaNatacion.Application.App;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using QRCoder;

namespace AsistenciaNatacion
{
    public static class WhatsAppHelper
    {
        public static Bitmap GenerarImagenQR(string codigo)
        {
            using var generador = new QRCodeGenerator();

            var datosQr = generador.CreateQrCode(
                codigo,
                QRCodeGenerator.ECCLevel.Q);

            using var qrCode = new QRCode(datosQr);

            return qrCode.GetGraphic(12);
        }

        public static void EnviarBienvenida(Alumno alumno)
        {
            if (alumno == null)
                return;

            if (string.IsNullOrWhiteSpace(alumno.Telefono))
            {
                MessageBox.Show(
                    "El alumno no tiene un número de teléfono registrado.",
                    "Sin Teléfono",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string telefonoLimpio =
                FormatearTelefonoVenezuela(alumno.Telefono);

            if (string.IsNullOrWhiteSpace(telefonoLimpio))
            {
                MessageBox.Show(
                    "El número de teléfono registrado no es válido.",
                    "Teléfono Inválido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            bool qrCopiado = false;

            try
            {
                using Bitmap qrBitmap =
                    GenerarImagenQR(alumno.Codigo);

                Clipboard.SetImage(qrBitmap);

                qrCopiado = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo copiar el QR al portapapeles:\n\n{ex.Message}",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            var configuracion =
                AppServices.CrearObtenerConfiguracion()
                    .Ejecutar(alumno.ActividadId);

            string nombreEstablecimiento =
                string.IsNullOrWhiteSpace(configuracion.Nombre)
                    ? "Establecimiento"
                    : configuracion.Nombre;

            string tipoEstablecimiento =
                string.IsNullOrWhiteSpace(
                    configuracion.TipoEstablecimiento)
                    ? "Actividad"
                    : configuracion.TipoEstablecimiento;

            var plan =
                AppServices.CrearObtenerPlanActividadPorId()
                    .Ejecutar(alumno.PlanId);

            string descripcionPlan =
                plan?.DescripcionCompleta ?? "Sin plan";

            string mensaje =
                $"¡Hola *{alumno.Nombre}*! 👋🏼\n\n" +
                $"¡Bienvenido(a) a *{nombreEstablecimiento}*! ✨\n" +
                $"🏷️ *Actividad:* {tipoEstablecimiento}\n\n" +
                $"📌 *Detalles de tu plan:*\n" +
                $"• *Plan:* {descripcionPlan}\n" +
                (alumno.ClasesMes > 0
                    ? $"• *Clases del mes:* {alumno.ClasesMes} clases\n"
                    : "") +
                $"• *Código de alumno:* `{alumno.Codigo}`\n\n" +
                (qrCopiado
                    ? "📲 Te adjuntamos tu *Código QR de acceso*.\n" +
                      "Preséntalo al ingresar al establecimiento."
                    : "📲 Recuerda tu código para el registro de asistencia al ingresar.");

            AbrirWhatsApp(
                telefonoLimpio,
                mensaje);

            if (qrCopiado)
            {
                MessageBox.Show(
                    "Se abrió WhatsApp con el mensaje de bienvenida.\n\n" +
                    "El Código QR está copiado en el portapapeles.\n\n" +
                    "En el chat de WhatsApp presiona Ctrl + V para pegarlo y enviarlo.",
                    "Enviar Bienvenida y QR",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        public static void EnviarRecibo(
            Alumno alumno,
            Pago pago,
            decimal cuotaTotal,
            decimal totalAbonado,
            decimal restante)
        {
            if (alumno == null || pago == null)
                return;

            if (string.IsNullOrWhiteSpace(alumno.Telefono))
            {
                MessageBox.Show(
                    "El alumno no tiene un número de teléfono registrado.",
                    "Sin Teléfono",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string telefonoLimpio =
                FormatearTelefonoVenezuela(
                    alumno.Telefono);

            if (string.IsNullOrWhiteSpace(telefonoLimpio))
            {
                MessageBox.Show(
                    "El número de teléfono registrado no es válido.",
                    "Teléfono Inválido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            var configuracion =
                AppServices.CrearObtenerConfiguracion()
                    .Ejecutar(alumno.ActividadId);

            string nombreEstablecimiento =
                string.IsNullOrWhiteSpace(configuracion.Nombre)
                    ? "Establecimiento"
                    : configuracion.Nombre;

            string actividad =
                string.IsNullOrWhiteSpace(
                    configuracion.TipoEstablecimiento)
                    ? "Actividad"
                    : configuracion.TipoEstablecimiento;

            string fecha =
                pago.FechaPago.ToString(
                    "dd/MM/yyyy hh:mm tt");

            string mensaje = $"*COMPROBANTE DE PAGO*\n" +
                             $"*{nombreEstablecimiento}*\n" +
                             $"Actividad: {actividad}\n\n" +
                             $"*Folio:* {pago.Folio}\n" +
                             $"*Fecha:* {fecha}\n\n" +
                             $"*Alumno:* {alumno.Nombre}\n" +
                             $"*Código:* {alumno.Codigo}\n" +
                             $"*Período:* {pago.Mes:D2}/{pago.Anio}\n\n" +
                             $"*Monto abonado:* ${pago.MontoUsd:N2}\n" +
                             $"*Tasa BCV:* {pago.TasaBcv:N2} Bs/$\n" +
                             $"*Equivalente:* {pago.MontoBs:N2} Bs.\n\n" +
                             $"*Método de pago:* {pago.MetodoPago}\n" +
                             $"*Referencia:* {(string.IsNullOrWhiteSpace(pago.Referencia) ? "N/A" : pago.Referencia)}\n\n" +
                             $"*Cuota total:* ${cuotaTotal:N2}\n" +
                             $"*Total abonado:* ${totalAbonado:N2}\n" +
                             $"*Resta por pagar:* ${restante:N2}\n\n" +
                             $"¡Muchas gracias por su pago!\n" +
                             $"Conserve este mensaje como comprobante.";

            AbrirWhatsApp(
                telefonoLimpio,
                mensaje);
        }

        private static void AbrirWhatsApp(
            string telefono,
            string mensaje)
        {
            string urlApp = $"whatsapp://send?phone={telefono}&text={Uri.EscapeDataString(mensaje)}";
            string urlWeb = $"https://api.whatsapp.com/send?phone={telefono}&text={Uri.EscapeDataString(mensaje)}";

            bool isAppInstalled = false;
            try
            {
                // Verificamos si el esquema de la aplicación "whatsapp://" está registrado.
                using (var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("whatsapp"))
                {
                    isAppInstalled = (key != null);
                }
            }
            catch { } // Ignoramos posibles errores de permisos al leer el registro.

            if (isAppInstalled)
            {
                try
                {
                    // Intento 1: Llamada nativa estándar
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = urlApp,
                            UseShellExecute = true
                        });
                }
                catch
                {
                    try
                    {
                        // Intento 2: Puente mediante explorer.exe
                        // Workaround: Soluciona la excepción cuando C# se ejecuta como Administrador
                        // o Windows bloquea el arranque directo de esquemas UWP (app de la Microsoft Store).
                        Process.Start(
                            new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = $"\"{urlApp}\"",
                                UseShellExecute = true
                            });
                    }
                    catch
                    {
                        // Solo si falla el puente de Explorer, procedemos a abrir la versión Web.
                        AbrirWhatsAppWeb(urlWeb);
                    }
                }
            }
            else
            {
                // Si la app de escritorio no está instalada, vamos directamente a la versión Web
                AbrirWhatsAppWeb(urlWeb);
            }
        }

        private static void AbrirWhatsAppWeb(string urlWeb)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = urlWeb,
                        UseShellExecute = true
                    });
            }
            catch (Exception exWeb)
            {
                MessageBox.Show(
                    $"No se pudo abrir WhatsApp ni WhatsApp Web.\n\n{exWeb.Message}",
                    "WhatsApp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string FormatearTelefonoVenezuela(
            string rawPhone)
        {
            string soloNumeros =
                Regex.Replace(
                    rawPhone,
                    @"[^\d]",
                    "");

            if (string.IsNullOrEmpty(soloNumeros))
                return "";

            if (soloNumeros.StartsWith("0") &&
                soloNumeros.Length == 11)
            {
                return "58" +
                       soloNumeros.Substring(1);
            }

            if (soloNumeros.Length == 10 &&
                (soloNumeros.StartsWith("4") ||
                 soloNumeros.StartsWith("2")))
            {
                return "58" + soloNumeros;
            }

            if (soloNumeros.StartsWith("58") &&
                soloNumeros.Length == 12)
            {
                return soloNumeros;
            }

            return soloNumeros;
        }
    }
}