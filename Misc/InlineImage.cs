//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Drawing;
using System.IO;
using TermSharp.Vt100;
using Xwt.Drawing;

namespace TermSharp.Misc
{
    public static class InlineImage
    {
        public static string Encode(Bitmap image)
        {
            using(var stream = new MemoryStream())
            {
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                var encodedImage = Convert.ToBase64String(stream.GetBuffer());
                return GenerateControlSequence(encodedImage);
            }
        }

        public static string Encode(BitmapImage image)
        {
            using(var stream = new MemoryStream())
            {
                image.Save(stream, ImageFileType.Jpeg);
                var encodedImage = Convert.ToBase64String(stream.GetBuffer());
                return GenerateControlSequence(encodedImage);
            }
        }

        private static string GenerateControlSequence(string encodedImage)
        {
            return $"{(char)ControlByte.Escape}{(char)ControlByte.OperatingSystemCommand}{InlineImageCode};File=inline=1:{encodedImage}{(char)ControlByte.Bell}";
        }

        public const int InlineImageCode = 1337;
    }
}
