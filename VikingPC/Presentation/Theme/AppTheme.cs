using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaNatacion.Presentation.Theme;

public static class AppTheme
{
    public static readonly Color Primary = ColorTranslator.FromHtml("#0B5394");
    public static readonly Color PrimaryDark = ColorTranslator.FromHtml("#073763");
    public static readonly Color Accent = ColorTranslator.FromHtml("#18A999");
    public static readonly Color Background = ColorTranslator.FromHtml("#F4F7FB");
    public static readonly Color Surface = Color.White;
    public static readonly Color Text = ColorTranslator.FromHtml("#172033");
    public static readonly Color Muted = ColorTranslator.FromHtml("#64748B");
    public static readonly Color Border = ColorTranslator.FromHtml("#D7E0EA");

    public static void Aplicar(Form form)
    {
        form.BackColor = Background;
        form.Font = new Font("Segoe UI", 9.5f);

        foreach (Control control in form.Controls)
            AplicarControl(control);
    }

    private static void AplicarControl(Control control)
    {
        if (control is Button button)
        {
            button.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 241, 250);
            button.Cursor = Cursors.Hand;
            if (button.BackColor == SystemColors.Control || button.BackColor == Color.Empty)
            {
                button.BackColor = Surface;
                button.ForeColor = Text;
            }
        }
        else if (control is TextBoxBase textBox)
        {
            textBox.BackColor = Surface;
            textBox.ForeColor = Text;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is ComboBox combo)
        {
            combo.BackColor = Surface;
            combo.ForeColor = Text;
            combo.FlatStyle = FlatStyle.Flat;
        }
        else if (control is Label label && label.ForeColor == SystemColors.ControlText)
        {
            label.ForeColor = Text;
        }
        else if (control is ListView listView)
        {
            listView.BackColor = Surface;
            listView.ForeColor = Text;
            listView.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is DataGridView grid)
        {
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grid.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#DCECF9");
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            grid.RowHeadersVisible = false;
        }

        foreach (Control child in control.Controls)
            AplicarControl(child);
    }
}
