using AsistenciaNatacion.Application.App;
﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using QRCoder;

namespace AsistenciaNatacion
{
    public static class WhatsAppHelper
    {
        /// <summary>
        /// Genera la imagen del QR usando QRCoder (misma lógica que QrForm).
        /// </summary>
        public static Bitmap GenerarImagenQR(string codigo)
        {
            using var generador = new QRCodeGenerator();
            var datosQr = generador.CreateQrCode(codigo, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(datosQr);
            return qrCode.GetGraphic(12);
        }

        /// <summary>
        /// Abre WhatsApp con la bienvenida formateada y copia el QR al portapapeles.
        /// </summary>
        public static void EnviarBienvenida(Alumno alumno)
        {
            if (string.IsNullOrWhiteSpace(alumno.Telefono))
            {
                MessageBox.Show(
                    "El alumno no tiene un número de teléfono registrado.",
                    "Sin Teléfono",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string telefonoLimpio = FormatearTelefonoVenezuela(alumno.Telefono);
            if (string.IsNullOrWhiteSpace(telefonoLimpio))
            {
                MessageBox.Show(
                    "El número de teléfono registrado no es válido.",
                    "Teléfono Inválido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // 1. Generar el QR y copiarlo al portapapeles
            bool qrCopiado = false;
            try
            {
                using Bitmap qrBitmap = GenerarImagenQR(alumno.Codigo);
                Clipboard.SetImage(qrBitmap);
                qrCopiado = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo copiar el QR al portapapeles: {ex.Message}",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            var configuracion = AppServices.CrearObtenerConfiguracion().Ejecutar(alumno.ActividadId);

            string nombreEstablecimiento =
                configuracion.Nombre;

            string tipoEstablecimiento =
                configuracion.TipoEstablecimiento;

            // 2. Mensaje formateado de bienvenida
            string mensaje = $"¡Hola *{alumno.Nombre}*! 👋🏼\n\n" +
                 $"¡Bienvenido(a) a *{nombreEstablecimiento}*! ✨\n" +
                 $"🏷️ *Actividad:* {tipoEstablecimiento}\n\n" +
                 $"📌 *Detalles de tu plan:*\n" +
                 $"• *Plan:* {AppServices.CrearObtenerPlanActividadPorId().Ejecutar(alumno.PlanId)?.DescripcionCompleta ?? "Sin plan"}\n" +
                 (alumno.ClasesMes > 0 ? $"• *Clases del mes:* {alumno.ClasesMes} clases\n" : "") +
                 $"• *Código de alumno:* `{alumno.Codigo}`\n\n" +
                 (qrCopiado
                     ? "📲 Te adjuntamos tu *Código QR de acceso*. Preséntalo al ingresar al establecimiento."
                     : "📲 Recuerda tu código para el registro de asistencia al ingresar.");

            // 3. Abrir WhatsApp
            string url = $"https://api.whatsapp.com/send?phone={telefonoLimpio}&text={Uri.EscapeDataString(mensaje)}";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });

                if (qrCopiado)
                {
                    MessageBox.Show(
                        "Se ha abierto WhatsApp con el mensaje de bienvenida.\n\n" +
                        "💡 *Instrucciones:* La imagen de su Código QR ya está copiada en tu portapapeles.\n" +
                        "En el chat de WhatsApp, solo presiona **Ctrl + V** para pegarla y enviarla.",
                        "Enviar Bienvenida y QR",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al abrir WhatsApp: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string FormatearTelefonoVenezuela(string rawPhone)
        {
            string soloNumeros = Regex.Replace(rawPhone, @"[^\d]", "");
            if (string.IsNullOrEmpty(soloNumeros)) return "";

            if (soloNumeros.StartsWith("0") && soloNumeros.Length == 11)
                return "58" + soloNumeros.Substring(1);

            if (soloNumeros.Length == 10 && (soloNumeros.StartsWith("4") || soloNumeros.StartsWith("2")))
                return "58" + soloNumeros;

            if (soloNumeros.StartsWith("58") && soloNumeros.Length == 12)
                return soloNumeros;

            return soloNumeros;
        }

        public static void EnviarRecibo(
    Alumno alumno,
    Pago pago,
    decimal cuotaTotal,
    decimal totalAbonado,
    decimal restante)
        {
            if (string.IsNullOrWhiteSpace(alumno.Telefono))
            {
                MessageBox.Show(
                    "El alumno no tiene un número de teléfono registrado.",
                    "Sin Teléfono",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string telefonoLimpio = FormatearTelefonoVenezuela(
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

            string fecha = pago.FechaPago.ToString(
                "dd/MM/yyyy hh:mm tt");

            var configuracion = AppServices.CrearObtenerConfiguracion().Ejecutar(alumno.ActividadId);
            string nombreEstablecimiento = string.IsNullOrWhiteSpace(configuracion.Nombre)
                ? "Establecimiento"
                : configuracion.Nombre;
            string actividad = string.IsNullOrWhiteSpace(configuracion.TipoEstablecimiento)
                ? "Actividad"
                : configuracion.TipoEstablecimiento;

            string mensaje =
                $"🧾 *COMPROBANTE DE PAGO*\n" +
                $"🏷️ *{nombreEstablecimiento}*\n" +
                $"🏋️ *Actividad:* {actividad}\n\n" +
                $"*Folio:* {pago.Folio}\n" +
                $"*Fecha:* {fecha}\n\n" +
                $"👤 *Alumno:* {alumno.Nombre}\n" +
                $"*Código:* {alumno.Codigo}\n" +
                $"*Período:* {pago.Mes:D2}/{pago.Anio}\n\n" +
                $"💵 *Monto abonado:* ${pago.MontoUsd:N2}\n" +
                $"💱 *Tasa BCV:* {pago.TasaBcv:N2} Bs/$\n" +
                $"🇻🇪 *Equivalente:* {pago.MontoBs:N2} Bs.\n\n" +
                $"💳 *Método de pago:* {pago.MetodoPago}\n" +
                $"🔢 *Referencia:* {(string.IsNullOrWhiteSpace(pago.Referencia) ? "N/A" : pago.Referencia)}\n\n" +
                $"*Cuota total:* ${cuotaTotal:N2}\n" +
                $"*Total abonado:* ${totalAbonado:N2}\n" +
                $"*Resta por pagar:* ${restante:N2}\n\n" +
                $"¡Muchas gracias por su pago! 🙏\n" +
                $"Conserve este mensaje como comprobante.";

            string url =
                $"https://api.whatsapp.com/send?phone={telefonoLimpio}" +
                $"&text={Uri.EscapeDataString(mensaje)}";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al abrir WhatsApp: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

    }
}