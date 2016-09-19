//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using Xwt;
using Xwt.Drawing;

namespace TermSharp.Misc
{
    internal static class Utilities
    {
        public static void Swap(ref int a, ref int b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        public static Size GetCharSizeFromLayoutParams(ILayoutParameters parameters)
        {
            var textLayout = GetTextLayoutFromLayoutParams(parameters);
            textLayout.Text = "a\na";
            return new Size(textLayout.GetCoordinateFromIndex(1).X, textLayout.GetCoordinateFromIndex(2).Y);
        }

        public static TextLayout GetTextLayoutFromLayoutParams(ILayoutParameters parameters)
        {
            var result = new TextLayout();
            result.Font = parameters.Font;
            return result;
        }

        public static Size GetLineSizeFromLayoutParams(ILayoutParameters parameters)
        {
            var textLayout = Utilities.GetTextLayoutFromLayoutParams(parameters);
            textLayout.Text = "a\na";
            return new Size(parameters.Width, textLayout.GetCoordinateFromIndex(2).Y);
        }
    }
}

