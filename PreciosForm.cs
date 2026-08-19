using AsistenciaNatacion.Application.UseCases;
using AsistenciaNatacion.Domain;
using AsistenciaNatacion.Application.App;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public class PreciosForm : Form
    {
        private readonly int actividadId;
        private readonly Dictionary<int, NumericUpDown> camposPorPlan = new();
        private readonly ComboBox cbMes;
        private readonly NumericUpDown nudAnio;
        private readonly List<PlanActividad> planes;
        public event Action? PreciosActualizados;

        public PreciosForm(int actividadId)
        {
            this.actividadId = actividadId;
            planes = AppServices.CrearObtenerPlanesPorActividad().Ejecutar(actividadId);

            Text = "💲 Ajustar precios por plan";
            Size = new Size(520, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; BackColor = Color.White;
            KeyPreview = true; KeyDown += Form_KeyDown;

            Controls.Add(new Label { Text = "Precios de planes", Font = new Font("Segoe UI",13,FontStyle.Bold), ForeColor=ColorTranslator.FromHtml("#0b5394"), AutoSize=true, Location=new Point(20,15)});
            Controls.Add(new Label { Text="Vigente a partir de:", Font=new Font("Segoe UI",9,FontStyle.Bold), AutoSize=true, Location=new Point(20,55)});
            cbMes = new ComboBox { Location=new Point(20,80), Width=150, DropDownStyle=ComboBoxStyle.DropDownList };
            cbMes.Items.AddRange(new object[]{"Enero","Febrero","Marzo","Abril","Mayo","Junio","Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre"});
            Controls.Add(cbMes);
            nudAnio = new NumericUpDown { Location=new Point(180,80), Width=100, Minimum=DateTime.Today.Year, Maximum=DateTime.Today.Year+10, Value=DateTime.Today.Year };
            Controls.Add(nudAnio);
            cbMes.SelectedIndex=DateTime.Today.Month-1; cbMes.SelectedIndexChanged += (s,e)=>CargarPrecios(); nudAnio.ValueChanged += (s,e)=>CargarPrecios();

            int y=125;
            foreach (var plan in planes)
            {
                Controls.Add(new Label { Text=$"{plan.Nombre} ($):", Location=new Point(20,y), AutoSize=true });
                var nud=new NumericUpDown { Location=new Point(300,y-3), Width=100, Minimum=0, Maximum=999999, DecimalPlaces=2 };
                Controls.Add(nud); camposPorPlan[plan.Id]=nud; y+=40;
            }
            if (planes.Count==0) Controls.Add(new Label { Text="No hay planes configurados para esta actividad.", Location=new Point(20,y), AutoSize=true, ForeColor=Color.Gray});
            y+=10;
            Controls.Add(new Label { Text="Los cambios afectan desde el mes seleccionado.", Font=new Font("Segoe UI",8), ForeColor=Color.Gray, AutoSize=true, Location=new Point(20,y)});
            y+=35;
            var btnGuardar=new Button { Text="Guardar", BackColor=ColorTranslator.FromHtml("#0b5394"), ForeColor=Color.White, Location=new Point(20,y), Width=110, Height=32 }; btnGuardar.Click+=(s,e)=>Guardar(); Controls.Add(btnGuardar);
            var btnCerrar=new Button { Text="Cerrar", Location=new Point(140,y), Width=110, Height=32 }; btnCerrar.Click+=(s,e)=>Close(); Controls.Add(btnCerrar);
            CargarPrecios();
        }

        private void CargarPrecios()
        {
            if (cbMes.SelectedIndex<0) return;
            int mes=cbMes.SelectedIndex+1, anio=(int)nudAnio.Value;
            foreach(var plan in planes) if(camposPorPlan.TryGetValue(plan.Id,out var nud)) nud.Value=AppServices.CrearObtenerPrecioPlan().Ejecutar(plan.Id,anio,mes);
        }

        private void Guardar()
        {
            if (planes.Count==0) return;
            int mes=cbMes.SelectedIndex+1, anio=(int)nudAnio.Value;
            int seleccionado=anio*100+mes, actual=DateTime.Today.Year*100+DateTime.Today.Month;
            if(seleccionado<actual){MessageBox.Show("No puedes modificar precios de meses anteriores. Selecciona el mes actual o un mes futuro.","Período inválido",MessageBoxButtons.OK,MessageBoxIcon.Warning); return;}
            foreach(var plan in planes) AppServices.CrearActualizarPrecioPlan().Ejecutar(plan.Id,camposPorPlan[plan.Id].Value,anio,mes,actividadId);
            PreciosActualizados?.Invoke();
            MessageBox.Show($"Precios actualizados correctamente.\n\nVigentes desde: {cbMes.SelectedItem} {anio}.","Listo",MessageBoxButtons.OK,MessageBoxIcon.Information);
            Close();
        }

        private void Form_KeyDown(object? sender, KeyEventArgs e){ if(e.KeyCode==Keys.Escape){e.Handled=true;e.SuppressKeyPress=true;Close();}}
    }
}
