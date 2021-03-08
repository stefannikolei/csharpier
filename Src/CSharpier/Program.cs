using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSharpier;
using UtfUnknown;

namespace CSharpier
{
    class Program
    {
        private static int sourceLost;
        private static int exceptionsFormatting;
        private static int exceptionsValidatingSource;
        private static int files;

        static async Task<int> Main(string[] args)
        {
            var rootCommand = CommandLineOptions.Create();

            rootCommand.Handler = CommandHandler.Create(
                new CommandLineOptions.Handler(Run));

            return await rootCommand.InvokeAsync(args);
        }

        // TODO if someone kills this process while running, I think it can leave files half written
        public static async Task<int> Run(string directory, bool validate)
        {
            var fullStopwatch = Stopwatch.StartNew();

            // TODO 1 Configuration.cs from data.entities
            // TODO 1 CurrencyDto.cs from data.entities
            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
            
#if DEBUG
                directory = GetTestingPath();
#endif
            }

            var tasks = Directory.EnumerateFiles(
                directory,
                "*.cs",
                SearchOption.AllDirectories).AsParallel().Select(
                o => DoWork(o, directory, validate)).ToArray();
            Task.WaitAll(tasks);
            Console.WriteLine(
                PadToSize("total time: ", 80) + ReversePad(
                    fullStopwatch.ElapsedMilliseconds + "ms"));
            Console.WriteLine(
                PadToSize("total files: ", 80) + ReversePad(files + "  "));
            if (validate)
            {
                Console.WriteLine(
                    PadToSize("files that failed syntax tree validation: ", 80) + ReversePad(
                        sourceLost + "  "));
                Console.WriteLine(
                    PadToSize(
                        "files that threw exceptions while formatting: ",
                        80) + ReversePad(exceptionsFormatting + "  "));
                Console.WriteLine(
                    PadToSize(
                        "files that threw exceptions while validating syntax tree: ",
                        80) + ReversePad(exceptionsValidatingSource + "  "));
            }

            return 0;
        }

        private static string GetTestingPath()
        {
            //var path = "C:\\temp\\clifiles";
            var path = @"c:\Projects\formattingTests\";
            //path += "Newtonsoft.Json";
            path += "insite-commerce-prettier";

            // TODO 0 why does that weird file in roslyn fail validation?
            // also what about the files that fail to compile?
            //path = +="roslyn";
            // TODO 0 lots fail to compile, codegen files should be excluded perhaps?
            //path += "spnetcore";
            // TODO 0 stackoverflows, probably because of the ridiculous testing files that are in there, and c# thinks there is a stackoverflow because the stack is so large, and looks like it is repeating.
            // actually it looks like runtime has tests that will fail in recursive tree walkers
            // see https://github.com/dotnet/runtime/blob/master/src/tests/JIT/Regression/JitBlue/GitHub_10215/GitHub_10215.cs
            // TODO 0 also some weird "failures" due to trivia moving lines, although the compiled code would be the same
            //path += "runtime";
            //path += "AspNetWebStack";
            return path;
        }

        private static async Task DoWork(string file, string path, bool validate)
        {
            if (file.EndsWith(".g.cs") || file.EndsWith(".cshtml.cs"))
            {
                return;
            }

            files++;

            using var reader = new StreamReader(file);
            var code = await reader.ReadToEndAsync();
            var detectionResult = CharsetDetector.DetectFromFile(file);
            var encoding = detectionResult.Detected.Encoding;
            reader.Close();

            CSharpierResult result;

            string GetPath()
            {
                return PadToSize(file.Substring(path.Length));
            }

            try
            {
                result = new CodeFormatter().Format(code, new Options());
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    GetPath() + " - threw exception while formatting");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                exceptionsFormatting++;
                return;
            }


            if (result.Errors.Any())
            {
                Console.WriteLine(GetPath() + " - failed to compile");
                return;
            }

            if (!result.FailureMessage.IsBlank())
            {
                Console.WriteLine(GetPath() + " - " + result.FailureMessage);
                return;
            }

            // TODO 1 use async inside of codeformatter?
            if (validate)
            {
                var syntaxNodeComparer = new SyntaxNodeComparer(
                    code,
                    result.Code);

                try
                {
                    var failure = syntaxNodeComparer.CompareSource();
                    if (!string.IsNullOrEmpty(failure))
                    {
                        sourceLost++;
                        Console.WriteLine(
                            GetPath() + " - failed syntax tree validation");
                        Console.WriteLine(failure);
                    }
                }
                catch (Exception ex)
                {
                    exceptionsValidatingSource++;
                    Console.WriteLine(
                        GetPath() + " - failed with exception during syntax tree validation" + Environment.NewLine + ex.Message + ex.StackTrace);
                }
            }

            await using var stream = File.Open(file, FileMode.Create);
            await using var writer = new StreamWriter(stream, encoding);
            await writer.WriteAsync(result.Code);
        }

        private static string PadToSize(string value, int size = 120)
        {
            while (value.Length < size)
            {
                value += " ";
            }

            return value;
        }

        private static string ReversePad(string value)
        {
            while (value.Length < 10)
            {
                value = " " + value;
            }

            return value;
        }
    }
}
