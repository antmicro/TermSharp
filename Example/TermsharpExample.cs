//
// Copyright (c) 2010-2021 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Xwt;

namespace TermSharp.Example
{
    public class TermsharpExample
    {
        public static void Main(string[] args)
        {
        #if NET
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assembly = Assembly.LoadFrom(Path.Combine(assemblyLocation, "Xwt.Gtk3.dll"));
            DllMap.Register(assembly);

            Application.Initialize(ToolkitType.Gtk3);
        #else
            Application.Initialize(ToolkitType.Gtk);
        #endif
            var window = new Window();
            window.Title = "Termsharp";

            terminalWidget = new TerminalWidget(ReceiveInput);
            window = new Window();
            window.Width = 1700;
            window.Height = 1400;
            window.Padding = new WidgetSpacing();
            window.Content = terminalWidget;
            window.CloseRequested += (_, __) => Application.Exit();
            window.Show();

            var commandThread = new Thread(RunCommand)
            {
                Name = "commandThread",
                IsBackground = true
            };
            commandThread.Start();

            Application.Run();
            window.Dispose();
            Application.Dispose();
        }

        private static void ReceiveInput(string input)
        {
            commandProcess.StandardInput.Write(input);
        }

        private static void RunCommand()
        {
            var command = "bash";
            commandProcess = new Process();
            commandProcess.EnableRaisingEvents = true;

            commandProcess.StartInfo = new ProcessStartInfo(command, "-i -c \"bash -i 2>&1\"")
            {
                UseShellExecute = false,
                //We're ignoring stderr explicitly
                //RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
            };
            commandProcess.Exited += (sender, e) =>
            {
                Application.Invoke(() => Application.Exit());
            };
            commandProcess.Start();
            Task.Run(() => ProcessOutput(commandProcess.StandardOutput.BaseStream)).Wait();
        }

        private static void ProcessOutput(Stream reader)
        {
            int readChar;
            while(-1 != (readChar = reader.ReadByte()))
            {
                if(readChar == '\n')
                    terminalWidget.Feed((byte)'\r');
                terminalWidget.Feed((byte)readChar);
            }
        }

        private static TerminalWidget terminalWidget;
        private static Process commandProcess;
    }
}
