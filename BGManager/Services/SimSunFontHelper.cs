using System.Drawing;

namespace BGManager.Services;

public static class SimSunFontHelper
{
    public static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font("宋体", size, style, GraphicsUnit.Point);
    }

    public static Font CreateFont(float size)
    {
        return CreateFont(size, FontStyle.Regular);
    }

    public static Font CreateFont(float size, bool bold)
    {
        return CreateFont(size, bold ? FontStyle.Bold : FontStyle.Regular);
    }

    public static string FontFamilyName => "宋体";
}