using System.ComponentModel;

namespace SqlNotebook.Import;

public enum ImportConversionFailOption
{
    [Description("Import the value as text")]
    ImportAsText = 1,

    [Description("Skip the row")]
    SkipRow = 2,

    [Description("Stop import with error")]
    Abort = 3,
}
