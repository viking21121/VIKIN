using System;
using System.IO;
using System.Net;
using System.Text;
using QRCoder;

namespace AsistenciaNatacion.Infrastructure.Carnet;

/// <summary>
/// Carnet de atleta de dos caras (tamaño tarjeta CR80, 85,6 × 54 mm) en HTML.
/// Cada cara sale en su propia página al convertir a PDF.
/// El QR contiene solo el código del alumno, igual que antes, para no romper
/// el escáner de VikingMobile ni el de escritorio.
/// </summary>
public static class GeneradorCarnet
{
    public static string Generar(Alumno alumno)
    {
        var datos = DatosCarnetRepositorio.Obtener(alumno.Id);
        string academia = DatosCarnetRepositorio.NombreAcademia(alumno.ActividadId);
        string disciplinaPrincipal = DatosCarnetRepositorio.NombreActividad(alumno.ActividadId);
        string disciplinas = string.Join(" · ", DatosCarnetRepositorio.Disciplinas());
        var (logo, logoTipo) = DatosCarnetRepositorio.Logo(alumno.ActividadId);

        string logoHtml = logo == null
            ? ""
            : $"<img class='logo' src='data:{E(logoTipo)};base64,{Convert.ToBase64String(logo)}'/>";

        string fotoHtml = "<div class='foto sin'>SIN<br/>FOTO</div>";
        string? rutaFoto = ResolverFoto(alumno);
        if (rutaFoto != null)
        {
            string ext = Path.GetExtension(rutaFoto).ToLowerInvariant();
            string mime = ext == ".png" ? "image/png" : "image/jpeg";
            fotoHtml = $"<img class='foto' src='data:{mime};base64,{Convert.ToBase64String(File.ReadAllBytes(rutaFoto))}'/>";
        }

        string nivel = string.IsNullOrWhiteSpace(datos.NivelCategoria) ? "" : datos.NivelCategoria;
        string linea = string.IsNullOrWhiteSpace(nivel)
            ? E(disciplinaPrincipal)
            : E(disciplinaPrincipal) + " · " + E(nivel);

        bool vigente = alumno.Activo &&
            (!alumno.FechaVencimientoPlan.HasValue || alumno.FechaVencimientoPlan.Value.Date >= DateTime.Today);
        string estado = vigente ? "ATLETA ACTIVO" : (alumno.Activo ? "PLAN VENCIDO" : "INACTIVO");
        string vigencia = alumno.FechaVencimientoPlan.HasValue
            ? "Vigente hasta " + alumno.FechaVencimientoPlan.Value.ToString("dd/MM/yyyy")
            : "";

        string sangre = string.IsNullOrWhiteSpace(datos.TipoSangre) ? "—" : E(datos.TipoSangre);
        string alertas = string.IsNullOrWhiteSpace(datos.AlertasMedicas) ? "Ninguna registrada" : E(datos.AlertasMedicas);
        string contacto = string.IsNullOrWhiteSpace(datos.ContactoNombre) ? "—" : E(datos.ContactoNombre);
        string telefono = string.IsNullOrWhiteSpace(datos.ContactoTelefono) ? "—" : E(datos.ContactoTelefono);
        string cedula = string.IsNullOrWhiteSpace(alumno.Cedula) ? "—" : E(alumno.Cedula);

        string qr = QrBase64(alumno.Codigo);
        string claseEstado = vigente ? "" : " no";
        string emitido = DateTime.Today.ToString("dd/MM/yyyy");
        string pieEstado = vigencia == "" ? "Emitido: " + E(emitido) : "Emitido: " + E(emitido) + "<br/>" + E(vigencia);

        var sb = new StringBuilder();
        sb.Append(@"<!DOCTYPE html><html><head><meta charset='utf-8'><title>Carnet</title><style>
@page { size: 85.6mm 54mm; margin: 0; }
* { box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
:root { --navy: #04152F; --navy2: #0B2F63; --azul: #1E6FD9; --oro: #D4A935; --texto: #1d2733; --suave: #6b7686; }
body { margin: 0; font-family: 'Segoe UI', Arial, sans-serif; background: #dfe4ea; }
.cara { width: 85.6mm; height: 54mm; position: relative; overflow: hidden;
        page-break-after: always; break-after: page; margin: 0; }
@media screen { .cara { margin: 12px auto; box-shadow: 0 3px 14px rgba(0,0,0,.3); border-radius: 3mm; } }

/* ---------- FRENTE ---------- */
.frente { background: linear-gradient(135deg, var(--navy) 0%, var(--navy2) 70%, #164a9a 100%); color: #fff; }
.frente::before { content: ''; position: absolute; right: -14mm; top: -18mm; width: 46mm; height: 46mm;
                  border-radius: 50%; background: rgba(255,255,255,.05); }
.frente::after { content: ''; position: absolute; right: -6mm; bottom: -22mm; width: 40mm; height: 40mm;
                 border-radius: 50%; background: rgba(212,169,53,.12); }
.cab { position: absolute; left: 4mm; right: 4mm; top: 3.2mm; display: flex; align-items: center; gap: 2.2mm; z-index: 2; }
.logo { height: 8.5mm; width: 8.5mm; object-fit: contain; background: #fff; border-radius: 50%; padding: .5mm; flex: none; }
.acad { font-weight: 700; font-size: 7.6pt; letter-spacing: .6px; text-transform: uppercase; line-height: 1.1; }
.disc { font-size: 4.8pt; letter-spacing: .9px; text-transform: uppercase; color: #9fc2f5; margin-top: .5mm; }
.linea { position: absolute; left: 4mm; right: 4mm; top: 13mm; height: .35mm; z-index: 2;
         background: linear-gradient(90deg, var(--oro), rgba(212,169,53,0)); }
.foto { position: absolute; left: 4mm; top: 16mm; width: 21mm; height: 27mm; object-fit: cover;
        border: .7mm solid #fff; border-radius: 1.8mm; background: #e8ecf2; z-index: 2;
        box-shadow: 0 0 0 .35mm var(--oro); }
.foto.sin { display: flex; align-items: center; justify-content: center; text-align: center;
            font-size: 5.5pt; color: #8a94a3; letter-spacing: .8px; }
.ident { position: absolute; left: 29mm; right: 4mm; top: 16mm; z-index: 2; }
.rotulo { font-size: 4.6pt; letter-spacing: 1.2px; text-transform: uppercase; color: var(--oro); font-weight: 700; }
.nombre { font-weight: 700; font-size: 10.5pt; line-height: 1.08; margin: .6mm 0 2.6mm; text-transform: uppercase; }
.par { margin-bottom: 1.7mm; }
.par .v { font-size: 8pt; font-weight: 600; letter-spacing: .3px; }
.cat { position: absolute; left: 29mm; right: 4mm; bottom: 4mm; z-index: 2; text-align: center;
       background: var(--oro); color: var(--navy); font-weight: 800; font-size: 6.6pt; letter-spacing: .9px;
       text-transform: uppercase; padding: 1.3mm 2mm; border-radius: 5mm; }

/* ---------- REVERSO ---------- */
.reverso { background: #fff; color: var(--texto); }
.top { position: absolute; left: 0; right: 0; top: 0; height: 7mm; background: linear-gradient(90deg, var(--navy), var(--navy2));
       color: #fff; display: flex; align-items: center; justify-content: space-between; padding: 0 4mm; }
.top .a { font-weight: 700; font-size: 6.4pt; letter-spacing: .7px; text-transform: uppercase; }
.top .b { font-size: 4.8pt; letter-spacing: 1.2px; text-transform: uppercase; color: var(--oro); font-weight: 700; }
.top::after { content: ''; position: absolute; left: 0; right: 0; bottom: 0; height: .5mm; background: var(--oro); }
.qrbox { position: absolute; left: 4mm; top: 9.5mm; width: 21mm; text-align: center; }
.qrmarco { border: .35mm solid var(--navy); border-radius: 1.8mm; padding: 1mm; background: #fff; }
.qr { width: 100%; display: block; }
.cod { margin-top: .6mm; font-size: 6.4pt; font-weight: 700; letter-spacing: 1px; color: var(--navy); }
.escan { font-size: 4.2pt; letter-spacing: .8px; text-transform: uppercase; color: var(--suave); }
.info { position: absolute; left: 29mm; right: 4mm; top: 9.5mm; }
.bl { margin-bottom: 1.8mm; padding-left: 1.8mm; border-left: .7mm solid var(--azul); }
.bl .t { font-size: 4.6pt; letter-spacing: 1px; text-transform: uppercase; color: var(--suave); font-weight: 700; }
.bl .v { font-size: 7pt; font-weight: 600; line-height: 1.2; }
.bl.med { border-left-color: #C0392B; }
.bl.med .t { color: #C0392B; }
.sangre { display: inline-block; background: #C0392B; color: #fff; font-weight: 800; font-size: 7pt;
          padding: .1mm 1.6mm; border-radius: 1mm; margin-right: 1.2mm; }
.estado { position: absolute; left: 4mm; right: 4mm; bottom: 7.4mm; display: flex; align-items: center; justify-content: space-between; }
.pill { background: #0B7A3B; color: #fff; font-weight: 800; font-size: 6pt; letter-spacing: .9px; padding: .9mm 3mm; border-radius: 4mm; }
.pill.no { background: #C0392B; }
.fechas { font-size: 5pt; color: var(--suave); text-align: right; line-height: 1.3; }
.legal { position: absolute; left: 0; right: 0; bottom: 0; height: 5.2mm; background: var(--navy); color: #cfd8e6;
         text-align: center; font-size: 4.6pt; display: flex; align-items: center; justify-content: center; padding: 0 3mm; }
.legal b { color: #fff; }
</style></head><body>");

        // FRENTE
        sb.Append($@"
<div class='cara frente'>
  <div class='cab'>{logoHtml}<div><div class='acad'>{E(academia)}</div><div class='disc'>{E(disciplinas)}</div></div></div>
  <div class='linea'></div>
  {fotoHtml}
  <div class='ident'>
    <div class='rotulo'>Atleta</div>
    <div class='nombre'>{E(alumno.Nombre)}</div>
    <div class='par'><div class='rotulo'>Cédula / ID</div><div class='v'>{cedula}</div></div>
    <div class='par'><div class='rotulo'>Código de atleta</div><div class='v'>{E(alumno.Codigo)}</div></div>
  </div>
  <div class='cat'>{linea}</div>
</div>");

        // REVERSO
        sb.Append($@"
<div class='cara reverso'>
  <div class='top'><div class='a'>{E(academia)}</div><div class='b'>Carnet de atleta</div></div>
  <div class='qrbox'>
    <div class='qrmarco'><img class='qr' src='data:image/png;base64,{qr}'/></div>
    <div class='cod'>{E(alumno.Codigo)}</div>
    <div class='escan'>Escanear para asistencia</div>
  </div>
  <div class='info'>
    <div class='bl'><div class='t'>Contacto de emergencia</div><div class='v'>{contacto}<br/>{telefono}</div></div>
    <div class='bl med'><div class='t'>Información médica</div><div class='v'><span class='sangre'>{sangre}</span>{alertas}</div></div>
  </div>
  <div class='estado'>
    <div class='pill{claseEstado}'>{estado}</div>
    <div class='fechas'>{pieEstado}</div>
  </div>
  <div class='legal'><span>Este carnet es personal e intransferible. Propiedad de <b>{E(academia)}</b>.</span></div>
</div>");

        sb.Append("</body></html>");

        string carpeta = Path.Combine(Path.GetTempPath(), "viking_carnets");
        Directory.CreateDirectory(carpeta);
        string seguro = string.Concat(alumno.Codigo.Split(Path.GetInvalidFileNameChars()));
        string ruta = Path.Combine(carpeta, $"carnet_{seguro}.html");
        File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(true));
        return ruta;
    }

    private static string? ResolverFoto(Alumno a)
    {
        if (!string.IsNullOrWhiteSpace(a.FotoRuta))
        {
            if (File.Exists(a.FotoRuta)) return a.FotoRuta;
            string rel = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, a.FotoRuta);
            if (File.Exists(rel)) return rel;
        }
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fotos");
        foreach (var ext in new[] { ".jpg", ".png", ".jpeg" })
        {
            string p = Path.Combine(dir, a.Codigo.Trim() + ext);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private static string QrBase64(string codigo)
    {
        using var gen = new QRCodeGenerator();
        var datos = gen.CreateQrCode(codigo, QRCodeGenerator.ECCLevel.Q);
        using var qr = new QRCode(datos);
        using var bmp = qr.GetGraphic(10);
        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
}
