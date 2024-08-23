using System.CommandLine;

namespace CSharpier.Cli;

using Microsoft.Extensions.Logging;

internal class CommandLineOptions
{
    public string[] DirectoryOrFilePaths { get; init; } = Array.Empty<string>();
    public bool Check { get; init; }
    public bool Fast { get; init; }
    public bool SkipWrite { get; init; }
    public bool WriteStdout { get; init; }
    public bool NoCache { get; init; }
    public bool NoMSBuildCheck { get; init; }
    public bool CompilationErrorsAsWarnings { get; init; }
    public bool IncludeGenerated { get; init; }
    public string? StandardInFileContents { get; init; }
    public string? ConfigPath { get; init; }
    public string[] OriginalDirectoryOrFilePaths { get; init; } = Array.Empty<string>();

    internal delegate Task<int> Handler(
        string[] directoryOrFile,
        bool check,
        bool fast,
        bool skipWrite,
        bool writeStdout,
        bool pipeMultipleFiles,
        bool server,
        int? serverPort,
        bool noCache,
        bool noMSBuildCheck,
        bool includeGenerated,
        bool compilationErrorsAsWarnings,
        string config,
        LogLevel logLevel,
        CancellationToken cancellationToken
    );

    internal delegate Task<int> CheckHandler(
        string[] directoryOrFile,
        string config,
        LogLevel logLevel,
        CancellationToken cancellationToken
    );

    private static Option GetLogLevelOption()
    {
        return new Option<string>(
            new[] { "--loglevel" },
            () => LogLevel.Information.ToString(),
            "Specify the log level - Debug, Information (default), Warning, Error, None"
        );
    }

    public static RootCommand Create()
    {
        var rootCommand = new RootCommand
        {
            GetDirectoryOrFileArgument(),
            GetLogLevelOption(),
            new Option(
                new[] { "--no-cache" },
                "Bypass the cache to determine if a file needs to be formatted."
            ),
            new Option(
                new[] { "--no-msbuild-check" },
                "Bypass the check to determine if a csproj files references a different version of CSharpier.MsBuild."
            ),
            new Option(
                new[] { "--include-generated" },
                "Include files generated by the SDK and files that begin with <autogenerated /> comments"
            ),
            new Option(
                new[] { "--fast" },
                "Skip comparing syntax tree of formatted file to original file to validate changes."
            ),
            new Option(
                new[] { "--skip-write" },
                "Skip writing changes. Generally used for testing to ensure csharpier doesn't throw any errors or cause syntax tree validation failures."
            ),
            new Option(
                new[] { "--write-stdout" },
                "Write the results of formatting any files to stdout."
            ),
            new Option(
                new[] { "--pipe-multiple-files" },
                "Keep csharpier running so that multiples files can be piped to it via stdin."
            ),
            new Option(
                new[] { "--server" },
                "Run CSharpier as a server so that multiple files may be formatted."
            ),
            new Option<int?>(
                new[] { "--server-port" },
                "Specify the port that CSharpier should start on. Defaults to a random unused port."
            ),
            new Option(
                new[] { "--compilation-errors-as-warnings" },
                "Treat compilation errors from files as warnings instead of errors."
            ),
        };

        rootCommand.AddGlobalOption(
            new Option<string>(
                new[] { "--config-path" },
                "Path to the CSharpier configuration file"
            )
        );

        rootCommand.AddValidator(cmd =>
        {
            if (!Console.IsInputRedirected && cmd.Children.Contains("--pipe-multiple-files"))
            {
                return "--pipe-multiple-files may only be used if you pipe stdin to CSharpier";
            }
            if (
                !Console.IsInputRedirected
                && !cmd.Children.Contains("directoryOrFile")
                && !cmd.Children.Contains("--server")
            )
            {
                return "directoryOrFile is required when not piping stdin to CSharpier";
            }

            return null;
        });

        return rootCommand;
    }

    private static Argument<string[]> GetDirectoryOrFileArgument()
    {
        return new Argument<string[]>("directoryOrFile")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description =
                "One or more paths to a directory containing C# files to format or a C# file to format. It may be ommited when piping data via stdin.",
        }.LegalFilePathsOnly();
    }

    public static Command CreateCheckCommand()
    {
        var checkCommand = new Command(
            "check",
            "Check that files are formatted. Will not write any changes."
        );
        checkCommand.AddArgument(GetDirectoryOrFileArgument());
        checkCommand.AddOption(GetLogLevelOption());

        return checkCommand;
    }
}
