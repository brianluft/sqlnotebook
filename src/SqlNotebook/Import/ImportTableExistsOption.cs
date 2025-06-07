using System.ComponentModel;

namespace SqlNotebook.Import;

public enum ImportTableExistsOption
{
    [Description("Append new rows")]
    AppendNewRows,

    [Description("Delete existing rows")]
    DeleteExistingRows,

    [Description("Drop table and re-create")]
    DropTable,
}
