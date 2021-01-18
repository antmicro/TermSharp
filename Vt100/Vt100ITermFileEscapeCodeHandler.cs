//
// Copyright (c) Antmicro
//
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.IO;
using System.Collections.Generic;
using Xwt.Drawing;

namespace TermSharp.Vt100
{
    public class Vt100ITermFileEscapeCodeHandler
    {
        public static bool TryParse(string encodedCommand, out Vt100ITermFileEscapeCodeHandler handler)
        {
            handler = new Vt100ITermFileEscapeCodeHandler();

            if(!encodedCommand.StartsWith(FileCommandHeader))
            {
                handler.Error = $"Unexpected command type: {encodedCommand}";
                return false;
            }

            // no, this is not a typo - we are splittin on `:` because the full command looks like:
            // ESC ] 1337 ; File = [arguments] : base-64 encoded file contents ^G
            // for details see: https://iterm2.com/documentation-images.html
            var split = encodedCommand.Split(new [] { ':' }, 2);
            if(split.Length != 2)
            {
                handler.Error = $"Unexpected command format: {encodedCommand}";
                return false;
            }

            if(!handler.TryParseArguments(split[0].Substring(FileCommandHeader.Length)))
            {
                return false;
            }

            if(!handler.TryParseImage(split[1]))
            {
                return false;
            }

            return true;
        }

        public string Error { get; private set; }
        public Image Image { get; private set; }

        private const string FileCommandHeader = "File=";

        private static bool InlineHandler(Vt100ITermFileEscapeCodeHandler handler, string argument)
        {
            if(argument == "0")
            {
                handler.Error = $"Only inline images are supported";
                return false;
            }

            return true;
        }

        private static bool Noop(Vt100ITermFileEscapeCodeHandler handler, string argument)
        {
            return true;
        }

        private bool TryParseArguments(string arguments)
        {
            if(arguments.Length == 0)
            {
                // nothing to parse
                return true;
            }

            var tags = arguments.Split(new [] { ';' });
            foreach(var tag in tags)
            {
                // name is base64 encoded and can contain the `=` character
                var splittedTag = tag.Split(new [] { '=' }, 2);
                if(splittedTag.Length != 2)
                {
                    Error = $"Unexpected tag format: {tag}";
                    return false;
                }

                if(!tagHandlers.TryGetValue(splittedTag[0], out var handler))
                {
                    Error = $"Unsupported tag: {(splittedTag[0])}";
                    return false;
                }

                if(!handler(this, splittedTag[1]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryParseImage(string encodedImage)
        {
            byte[] imageBytes = null;

            try
            {
                imageBytes = Convert.FromBase64String(encodedImage);
            }
            catch(FormatException e)
            {
                Error = $"Base64-encoded image format error: {(e.Message)}";
                return false;
            }

            try
            {
                using(var stream = new MemoryStream(imageBytes))
                {
                    Image = Image.FromStream(stream);
                }
            }
            catch(Exception e)
            {
                Error = $"Unsupported image format error: {(e.Message)}";
                return false;
            }

            return true;
        }

        private readonly Dictionary<string, Func<Vt100ITermFileEscapeCodeHandler, string, bool>> tagHandlers = new Dictionary<string, Func< Vt100ITermFileEscapeCodeHandler, string, bool>>
        {
            { "name", Noop },
            { "size", Noop },
            { "width", Noop },
            { "height", Noop },
            { "preserveAspectRatio", Noop },
            { "inline", InlineHandler }
        };
    }
}

