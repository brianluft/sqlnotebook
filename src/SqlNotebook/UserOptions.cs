using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using SqlNotebook.Properties;

namespace SqlNotebook;

public struct UserOptionsCachedFont
{
    public string Family;
    public float Size;
    public int Style;
    public Font Font;

    public static UserOptionsCachedFont Empty { get; } =
        new()
        {
            Family = "",
            Size = -1,
            Style = -1,
            Font = null,
        };
}

public static class UserOptionsInternal
{
    public static object Lock { get; } = new();
    public static UserOptionsCachedFont CachedDataTableFont { get; set; } = UserOptionsCachedFont.Empty;
    public static UserOptionsCachedFont CachedCodeFont { get; set; } = UserOptionsCachedFont.Empty;

    public static Font DefaultCodeFont { get; } = new("Consolas", 11f);
    public static Font DefaultDataTableFont { get; } = new("Segoe UI", 9f);
}

public sealed class UserOptionsColor
{
    // Indices into the Colors list
    public const int GRID_PLAIN = 0;
    public const int GRID_HEADER = 1;
    public const int GRID_LINES = 2;
    public const int GRID_BACKGROUND = 3;
    public const int CODE_PLAIN = 4;
    public const int CODE_KEYWORD = 5;
    public const int CODE_COMMENT = 6;
    public const int CODE_STRING = 7;
    public const int CODE_LINENUMS = 8;
    public const int CODE_BACKGROUND = 9;
    public const int NUM_COLORS = 10;

    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
}

public sealed class UserOptions
{
    public string DataTableFontFamily { get; set; }
    public float DataTableFontSize { get; set; }
    public int DataTableFontStyle { get; set; }

    public string CodeFontFamily { get; set; }
    public float CodeFontSize { get; set; }
    public int CodeFontStyle { get; set; }

    public List<UserOptionsColor> Colors { get; set; }

    // ---

    private static event EventHandler Updated;

    // This is called after the Updated event handlers finish.
    private static event EventHandler UpdatedPost;

    public static void OnUpdate(Component owner, Action action)
    {
        EventHandler handler = delegate
        {
            action();
        };
        Updated += handler;
        owner.Disposed += delegate
        {
            Updated -= handler;
        };
    }

    public static void OnUpdatePost(Component owner, Action action)
    {
        EventHandler handler = delegate
        {
            action();
        };
        UpdatedPost += handler;
        owner.Disposed += delegate
        {
            UpdatedPost -= handler;
        };
    }

    public static void OnUpdateAndNow(Component owner, Action action)
    {
        action();
        OnUpdate(owner, action);
    }

    public static readonly Lazy<UserOptions> _instance = new(() =>
    {
        try
        {
            var json = Settings.Default.UserOptions;
            if (!string.IsNullOrWhiteSpace(json))
            {
                return JsonSerializer.Deserialize<UserOptions>(json);
            }
        }
        catch { }
        return new();
    });
    public static UserOptions Instance => _instance.Value;

    public void Save()
    {
        Settings.Default.UserOptions = JsonSerializer.Serialize(this);
        Settings.Default.Save();
        Updated?.Invoke(this, EventArgs.Empty);
        UpdatedPost?.Invoke(this, EventArgs.Empty);
    }

    // ---

    public static Font GetDefaultDataTableFont() => UserOptionsInternal.DefaultDataTableFont;

    public Font GetDataTableFont()
    {
        lock (UserOptionsInternal.Lock)
        {
            var cache = GetFont(
                DataTableFontFamily,
                DataTableFontSize,
                DataTableFontStyle,
                UserOptionsInternal.CachedDataTableFont,
                UserOptionsInternal.DefaultDataTableFont
            );
            UserOptionsInternal.CachedDataTableFont = cache;
            return cache.Font;
        }
    }

    // Caller must call Save()
    public void SetDataTableFont(Font font)
    {
        lock (UserOptionsInternal.Lock)
        {
            DataTableFontFamily = font.FontFamily.Name;
            DataTableFontSize = font.Size;
            DataTableFontStyle = (int)font.Style;
            var cache = GetFont(
                DataTableFontFamily,
                DataTableFontSize,
                DataTableFontStyle,
                UserOptionsInternal.CachedDataTableFont,
                UserOptionsInternal.DefaultDataTableFont
            );
            UserOptionsInternal.CachedDataTableFont = cache;
        }
    }

    public static Font GetDefaultCodeFont() => UserOptionsInternal.DefaultCodeFont;

    public Font GetCodeFont()
    {
        lock (UserOptionsInternal.Lock)
        {
            var cache = GetFont(
                CodeFontFamily,
                CodeFontSize,
                CodeFontStyle,
                UserOptionsInternal.CachedCodeFont,
                UserOptionsInternal.DefaultCodeFont
            );
            UserOptionsInternal.CachedCodeFont = cache;
            return cache.Font;
        }
    }

    // Caller must call Save()
    public void SetCodeFont(Font font)
    {
        lock (UserOptionsInternal.Lock)
        {
            CodeFontFamily = font.FontFamily.Name;
            CodeFontSize = font.Size;
            CodeFontStyle = (int)font.Style;
            UserOptionsInternal.CachedCodeFont = GetFont(
                CodeFontFamily,
                CodeFontSize,
                CodeFontStyle,
                UserOptionsInternal.CachedCodeFont,
                UserOptionsInternal.DefaultCodeFont
            );
        }
    }

    private static UserOptionsCachedFont GetFont(
        string family,
        float size,
        int style,
        UserOptionsCachedFont cache,
        Font defaultFont
    )
    {
        if (string.IsNullOrWhiteSpace(family) || size <= 0)
        {
            return new()
            {
                Family = defaultFont.FontFamily.Name,
                Size = defaultFont.Size,
                Style = (int)defaultFont.Style,
                Font = defaultFont,
            };
        }

        if (cache.Font == null || cache.Family != family || cache.Size != size || cache.Style != style)
        {
            Font font = new(family, size, (FontStyle)style);
            return new()
            {
                Family = font.FontFamily.Name,
                Size = font.Size,
                Style = (int)font.Style,
                Font = font,
            };
        }

        return cache;
    }

    public static Color[] GetDefaultColors()
    {
        var colors = new Color[UserOptionsColor.NUM_COLORS];
        colors[UserOptionsColor.GRID_BACKGROUND] = Color.White;
        colors[UserOptionsColor.GRID_HEADER] = Color.FromArgb(221, 238, 255);
        colors[UserOptionsColor.GRID_LINES] = Color.FromArgb(0, 122, 204);
        colors[UserOptionsColor.GRID_PLAIN] = Color.Black;
        colors[UserOptionsColor.CODE_PLAIN] = Color.Black;
        colors[UserOptionsColor.CODE_COMMENT] = Color.Green;
        colors[UserOptionsColor.CODE_KEYWORD] = Color.Blue;
        colors[UserOptionsColor.CODE_STRING] = Color.Red;
        colors[UserOptionsColor.CODE_LINENUMS] = Color.LightGray;
        colors[UserOptionsColor.CODE_BACKGROUND] = Color.White;
        return colors;
    }

    public Color[] GetColors()
    {
        var colors = GetDefaultColors();
        if (Colors != null)
        {
            for (var i = 0; i < Colors.Count; i++)
            {
                var c = Colors[i];
                try
                {
                    colors[i] = Color.FromArgb(c.R, c.G, c.B);
                }
                catch { }
            }
        }
        return colors;
    }

    public void SetColors(Color[] colors)
    {
        Colors = colors
            .Select(x => new UserOptionsColor
            {
                R = x.R,
                G = x.G,
                B = x.B,
            })
            .ToList();
    }

    public string GetHexColor(int colorIndex)
    {
        var x = GetColors()[colorIndex];
        return $"#{x.R:X2}{x.G:X2}{x.B:X2}";
    }
}
