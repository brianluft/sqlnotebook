using System.Linq;
using System.Windows.Forms;

namespace SqlNotebook.Import.Csv;

public partial class ImportCsvPreviewControl : UserControl
{
    private readonly SqlTextControl _text;

    public string PreviewText
    {
        get { return _text.Text; }
        set
        {
            var truncatedText = string.Join(
                '\n',
                value.Split('\n').Select(x => x.Length > 1000 ? x.Substring(0, 1000) : x).Take(10_000)
            );

            _text.SqlText = truncatedText;
        }
    }

    public ImportCsvPreviewControl()
    {
        InitializeComponent();

        _text = new(true, false, false) { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
        Controls.Add(_text);
    }
}
