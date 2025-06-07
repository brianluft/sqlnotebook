using System.ComponentModel;

namespace SqlNotebook.Import;

public enum ColumnHeadersOption
{
    [Description("Cell range includes headers")]
    Present,

    [Description("No column headers")]
    NotPresent,
}
