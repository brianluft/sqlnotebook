using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SqlNotebookScript.Utils;

namespace SqlNotebook.Import.Csv;

public partial class ImportCsvOptionsControl : UserControl
{
    private readonly List<Tuple<int, string>> _encodings = new()
    {
        Tuple.Create(0, "Auto detect"),
        // populated in constructor
    };

    private readonly Tuple<ImportTableExistsOption, string>[] _ifExistsOptions = new[]
    {
        Tuple.Create(ImportTableExistsOption.AppendNewRows, "Append new rows"),
        Tuple.Create(ImportTableExistsOption.DeleteExistingRows, "Delete existing rows"),
        Tuple.Create(ImportTableExistsOption.DropTable, "Drop table and re-create"),
    };

    private readonly Tuple<ImportConversionFailOption, string>[] _conversionFailOptions = new[]
    {
        Tuple.Create(ImportConversionFailOption.ImportAsText, "Import the value as text"),
        Tuple.Create(ImportConversionFailOption.SkipRow, "Skip the row"),
        Tuple.Create(ImportConversionFailOption.Abort, "Stop import with error"),
    };

    private readonly Tuple<BlankValuesOption, string>[] _blankValuesOptions = new[]
    {
        Tuple.Create(BlankValuesOption.EmptyString, "Blank text"),
        Tuple.Create(BlankValuesOption.Null, "NULL"),
        Tuple.Create(BlankValuesOption.EmptyStringOrNull, "NULL for non-TEXT columns only"),
    };

    public Slot<int> SkipLines { get; } = new();
    public Slot<bool> HasColumnHeaders { get; } = new();
    public Slot<int> FileEncoding { get; } = new();
    public Slot<string> TargetTableName { get; } = new();
    public Slot<ImportTableExistsOption> IfTableExists { get; } = new();
    public Slot<ImportConversionFailOption> IfConversionFails { get; } = new();
    public Slot<BlankValuesOption> BlankValues { get; } = new();
    public Slot<string> Separator { get; } = new();

    public ImportCsvOptionsControl(DatabaseSchema schema)
    {
        InitializeComponent();

        Ui ui = new(this);
        ui.Init(_fileInputTitle);
        ui.Init(_sourceFlow);
        ui.Init(_skipLinesFlow);
        ui.Init(_skipLinesLabel);
        ui.Init(_skipLinesTxt);
        ui.MarginRight(_skipLinesTxt);
        ui.Init(_headerChk);
        ui.MarginTop(_headerChk);
        ui.Init(_encodingFlow);
        ui.Init(_encodingLabel);
        ui.Init(_encodingCmb, 45);
        ui.MarginRight(_encodingCmb);
        ui.Init(_separatorFlow);
        ui.Init(_separatorLabel);
        ui.Init(_separatorCombo, 10);
        ui.Init(_tableOutputTitle);
        ui.MarginTop(_tableOutputTitle);
        ui.Init(_tableLabel);
        ui.Init(_targetFlow);
        ui.Init(_tableNameFlow);
        ui.Init(_tableCmb, 40);
        ui.MarginRight(_tableCmb);
        ui.Init(_ifTableExistsLabel);
        ui.Init(_ifExistsFlow);
        ui.Init(_ifExistsCmb, 30);
        ui.Init(_convertFailFlow);
        ui.MarginRight(_convertFailFlow);
        ui.Init(_ifConversionFailsLabel);
        ui.Init(_convertFailCmb, 30);
        ui.Init(_targetFlow2);
        ui.MarginTop(_targetFlow2);
        ui.Init(_blankValuesFlow);
        ui.Init(_blankValuesLabel);
        ui.Init(_blankValuesCombo, 40);

        foreach (var tableName in schema.Tables.Keys)
        {
            _tableCmb.Items.Add(tableName);
        }

        // The System.Text.Encoding.CodePages package doesn't include useful display names.
        Dictionary<int, string> customEncodingNames = new()
        {
            [37] = "IBM EBCDIC (US-Canada)", // IBM037
            [437] = "OEM United States", // IBM437
            [500] = "IBM EBCDIC (International)", // IBM500
            [708] = "Arabic (ASMO 708)", // ASMO-708
            [720] = "Arabic (DOS)", // DOS-720
            [737] = "Greek (DOS)", // ibm737
            [775] = "Baltic (DOS)", // ibm775
            [850] = "Western European (DOS)", // ibm850
            [852] = "Central European (DOS)", // ibm852
            [855] = "OEM Cyrillic", // IBM855
            [857] = "Turkish (DOS)", // ibm857
            [858] = "OEM Multilingual Latin I", // IBM00858
            [860] = "Portuguese (DOS)", // IBM860
            [861] = "Icelandic (DOS)", // ibm861
            [862] = "Hebrew (DOS)", // DOS-862
            [863] = "French Canadian (DOS)", // IBM863
            [864] = "Arabic (864)", // IBM864
            [865] = "Nordic (DOS)", // IBM865
            [866] = "Cyrillic (DOS)", // cp866
            [869] = "Greek, Modern (DOS)", // ibm869
            [870] = "IBM EBCDIC (Multilingual Latin-2)", // IBM870
            [874] = "Thai (Windows)", // windows-874
            [875] = "IBM EBCDIC (Greek Modern)", // cp875
            [932] = "Japanese (Shift-JIS)", // shift_jis
            [936] = "Chinese Simplified (GB2312)", // gb2312
            [949] = "Korean", // ks_c_5601-1987
            [950] = "Chinese Traditional (Big5)", // big5
            [1026] = "IBM EBCDIC (Turkish Latin-5)", // IBM1026
            [1047] = "IBM Latin-1 (IBM01047)", // IBM01047
            [1140] = "IBM EBCDIC (US-Canada-Euro)", // IBM01140
            [1141] = "IBM EBCDIC (Germany-Euro)", // IBM01141
            [1142] = "IBM EBCDIC (Denmark-Norway-Euro)", // IBM01142
            [1143] = "IBM EBCDIC (Finland-Sweden-Euro)", // IBM01143
            [1144] = "IBM EBCDIC (Italy-Euro)", // IBM01144
            [1145] = "IBM EBCDIC (Spain-Euro)", // IBM01145
            [1146] = "IBM EBCDIC (UK-Euro)", // IBM01146
            [1147] = "IBM EBCDIC (France-Euro)", // IBM01147
            [1148] = "IBM EBCDIC (International-Euro)", // IBM01148
            [1149] = "IBM EBCDIC (Icelandic-Euro)", // IBM01149
            [1200] = "Unicode (UTF-16)", // utf-16
            [1201] = "Unicode (UTF-16 Big-Endian)", // unicodeFFFE
            [1250] = "Central European (Windows)", // windows-1250
            [1251] = "Cyrillic (Windows)", // windows-1251
            [1252] = "Western European (Windows)", // Windows-1252
            [1253] = "Greek (Windows)", // windows-1253
            [1254] = "Turkish (Windows)", // windows-1254
            [1255] = "Hebrew (Windows)", // windows-1255
            [1256] = "Arabic (Windows)", // windows-1256
            [1257] = "Baltic (Windows)", // windows-1257
            [1258] = "Vietnamese (Windows)", // windows-1258
            [1361] = "Korean (Johab)", // Johab
            [10000] = "Western European (Mac)", // macintosh
            [10001] = "Japanese (Mac)", // x-mac-japanese
            [10002] = "Chinese Traditional (Mac)", // x-mac-chinesetrad
            [10003] = "Korean (Mac)", // x-mac-korean
            [10004] = "Arabic (Mac)", // x-mac-arabic
            [10005] = "Hebrew (Mac)", // x-mac-hebrew
            [10006] = "Greek (Mac)", // x-mac-greek
            [10007] = "Cyrillic (Mac)", // x-mac-cyrillic
            [10008] = "Chinese Simplified (Mac)", // x-mac-chinesesimp
            [10010] = "Romanian (Mac)", // x-mac-romanian
            [10017] = "Ukrainian (Mac)", // x-mac-ukrainian
            [10021] = "Thai (Mac)", // x-mac-thai
            [10029] = "Central European (Mac)", // x-mac-ce
            [10079] = "Icelandic (Mac)", // x-mac-icelandic
            [10081] = "Turkish (Mac)", // x-mac-turkish
            [10082] = "Croatian (Mac)", // x-mac-croatian
            [12000] = "Unicode (UTF-32)", // utf-32
            [12001] = "Unicode (UTF-32 Big-Endian)", // utf-32BE
            [20000] = "Chinese Traditional (CNS)", // x-Chinese-CNS
            [20001] = "TCA Taiwan", // x-cp20001
            [20002] = "Chinese Traditional (Eten)", // x-Chinese-Eten
            [20003] = "IBM5550 Taiwan", // x-cp20003
            [20004] = "TeleText Taiwan", // x-cp20004
            [20005] = "Wang Taiwan", // x-cp20005
            [20105] = "Western European (IA5)", // x-IA5
            [20106] = "German (IA5)", // x-IA5-German
            [20107] = "Swedish (IA5)", // x-IA5-Swedish
            [20108] = "Norwegian (IA5)", // x-IA5-Norwegian
            [20127] = "US-ASCII", // us-ascii
            [20261] = "T.61", // x-cp20261
            [20269] = "ISO-6937", // x-cp20269
            [20273] = "IBM EBCDIC (Germany)", // IBM273
            [20277] = "IBM EBCDIC (Denmark-Norway)", // IBM277
            [20278] = "IBM EBCDIC (Finland-Sweden)", // IBM278
            [20280] = "IBM EBCDIC (Italy)", // IBM280
            [20284] = "IBM EBCDIC (Spain)", // IBM284
            [20285] = "IBM EBCDIC (UK)", // IBM285
            [20290] = "IBM EBCDIC (Japanese katakana)", // IBM290
            [20297] = "IBM EBCDIC (France)", // IBM297
            [20420] = "IBM EBCDIC (Arabic)", // IBM420
            [20423] = "IBM EBCDIC (Greek)", // IBM423
            [20424] = "IBM EBCDIC (Hebrew)", // IBM424
            [20833] = "IBM EBCDIC (Korean Extended)", // x-EBCDIC-KoreanExtended
            [20838] = "IBM EBCDIC (Thai)", // IBM-Thai
            [20866] = "Cyrillic (KOI8-R)", // koi8-r
            [20871] = "IBM EBCDIC (Icelandic)", // IBM871
            [20880] = "IBM EBCDIC (Cyrillic Russian)", // IBM880
            [20905] = "IBM EBCDIC (Turkish)", // IBM905
            [20924] = "IBM Latin-1 (IBM00924)", // IBM00924
            [20932] = "Japanese (JIS 0208-1990 and 0212-1990)", // EUC-JP
            [20936] = "Chinese Simplified (GB2312-80)", // x-cp20936
            [20949] = "Korean Wansung", // x-cp20949
            [21025] = "IBM EBCDIC (Cyrillic Serbian-Bulgarian)", // cp1025
            [21866] = "Cyrillic (KOI8-U)", // koi8-u
            [28591] = "Western European (ISO)", // iso-8859-1
            [28592] = "Central European (ISO)", // iso-8859-2
            [28593] = "Latin 3 (ISO)", // iso-8859-3
            [28594] = "Baltic (ISO)", // iso-8859-4
            [28595] = "Cyrillic (ISO)", // iso-8859-5
            [28596] = "Arabic (ISO)", // iso-8859-6
            [28597] = "Greek (ISO)", // iso-8859-7
            [28598] = "Hebrew (ISO-Visual)", // iso-8859-8
            [28599] = "Turkish (ISO)", // iso-8859-9
            [28603] = "Estonian (ISO)", // iso-8859-13
            [28605] = "Latin 9 (ISO)", // iso-8859-15
            [29001] = "Europa", // x-Europa
            [38598] = "Hebrew (ISO-Logical)", // iso-8859-8-i
            [50220] = "Japanese (JIS)", // iso-2022-jp
            [50221] = "Japanese (JIS-Allow 1 byte Kana)", // csISO2022JP
            [50222] = "Japanese (JIS-Allow 1 byte Kana - SO/SI)", // iso-2022-jp
            [50225] = "Korean (ISO)", // iso-2022-kr
            [50227] = "Chinese Simplified (ISO-2022)", // x-cp50227
            [51932] = "Japanese (EUC)", // euc-jp
            [51936] = "Chinese Simplified (EUC)", // EUC-CN
            [51949] = "Korean (EUC)", // euc-kr
            [52936] = "Chinese Simplified (HZ)", // hz-gb-2312
            [54936] = "Chinese Simplified (GB18030)", // GB18030
            [57002] = "ISCII Devanagari", // x-iscii-de
            [57003] = "ISCII Bengali", // x-iscii-be
            [57004] = "ISCII Tamil", // x-iscii-ta
            [57005] = "ISCII Telugu", // x-iscii-te
            [57006] = "ISCII Assamese", // x-iscii-as
            [57007] = "ISCII Oriya", // x-iscii-or
            [57008] = "ISCII Kannada", // x-iscii-ka
            [57009] = "ISCII Malayalam", // x-iscii-ma
            [57010] = "ISCII Gujarati", // x-iscii-gu
            [57011] = "ISCII Punjabi", // x-iscii-pa
            [65000] = "Unicode (UTF-7)", // utf-7
            [65001] = "Unicode (UTF-8)", // utf-8
        };
        _encodings.AddRange(
            from encoding in Encoding.GetEncodings()
            where encoding.CodePage > 0
            let name = customEncodingNames.GetValueOrDefault(encoding.CodePage) ?? encoding.DisplayName
            orderby name
            select Tuple.Create(encoding.CodePage, name)
        );
        _encodingCmb.ValueMember = "Item1";
        _encodingCmb.DisplayMember = "Item2";
        _encodingCmb.DataSource = _encodings;

        _ifExistsCmb.ValueMember = "Item1";
        _ifExistsCmb.DisplayMember = "Item2";
        _ifExistsCmb.DataSource = _ifExistsOptions;

        _convertFailCmb.ValueMember = "Item1";
        _convertFailCmb.DisplayMember = "Item2";
        _convertFailCmb.DataSource = _conversionFailOptions;

        _blankValuesCombo.ValueMember = "Item1";
        _blankValuesCombo.DisplayMember = "Item2";
        _blankValuesCombo.DataSource = _blankValuesOptions;

        _skipLinesTxt.BindValue(SkipLines);
        _headerChk.BindChecked(HasColumnHeaders);
        _encodingCmb.BindSelectedValue(FileEncoding);
        _tableCmb.BindText(TargetTableName);
        _ifExistsCmb.BindSelectedValue(IfTableExists);
        _convertFailCmb.BindSelectedValue(IfConversionFails);
        _blankValuesCombo.BindSelectedValue(BlankValues);
        _separatorCombo.BindText(Separator);

        _separatorCombo.SelectionLength = 0;
        BlankValues.Value = BlankValuesOption.Null;
    }

    public void SelectTableCombo()
    {
        _tableCmb.Select();
    }
}
