using System.CommandLine;
using System.CommandLine.Parsing;

namespace CSharpier.Cli;

using Microsoft.Extensions.Logging;

internal class CommandLineOptions
{
    public string[] DirectoryOrFilePaths { get; init; } = [];
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
    public string[] OriginalDirectoryOrFilePaths { get; init; } = [];

    internal delegate Task<int> FormatHandler(
        string[] directoryOrFile,
        bool fast,
        bool skipWrite,
        bool writeStdout,
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

    internal delegate Task<int> PipeHandler(
        string config,
        LogLevel logLevel,
        CancellationToken cancellationToken
    );

    internal delegate Task<int> ServerHandler(
        string config,
        int? serverPort,
        LogLevel logLevel,
        CancellationToken cancellationToken
    );

    public static RootCommand Create()
    {
        var rootCommand = new RootCommand();

        rootCommand.AddGlobalOption(
            new Option<string>(["--config-path"], "Path to the CSharpier configuration file")
        );

        rootCommand.AddGlobalOption(
            new Option<string>(
                ["--loglevel"],
                () => LogLevel.Information.ToString(),
                "Specify the log level - Debug, Information (default), Warning, Error, None"
            )
        );

        return rootCommand;
    }

    public static Command CreateFormatCommand()
    {
        var formatCommand = new Command("format", "Format files.");
        formatCommand.AddArgument(GetDirectoryOrFileArgument());
        formatCommand.AddValidator(ValidateDirectoryOrFiles());

        formatCommand.AddOption(
            new(["--no-cache"], "Bypass the cache to determine if a file needs to be formatted.")
        );

        formatCommand.AddOption(
            new(
                ["--no-msbuild-check"],
                "Bypass the check to determine if a csproj files references a different version of CSharpier.MsBuild."
            )
        );
        formatCommand.AddOption(
            new(
                ["--include-generated"],
                "Include files generated by the SDK and files that begin with <autogenerated /> comments"
            )
        );
        formatCommand.AddOption(
            new(
                ["--fast"],
                "Skip comparing syntax tree of formatted file to original file to validate changes."
            )
        );
        formatCommand.AddOption(
            new(
                ["--skip-write"],
                "Skip writing changes. Generally used for testing to ensure csharpier doesn't throw any errors or cause syntax tree validation failures."
            )
        );
        formatCommand.AddOption(
            new(["--write-stdout"], "Write the results of formatting any files to stdout.")
        );
        formatCommand.AddOption(
            new(
                ["--compilation-errors-as-warnings"],
                "Treat compilation errors from files as warnings instead of errors."
            )
        );

        return formatCommand;
    }

    public static Command CreateCheckCommand()
    {
        var checkCommand = new Command(
            "check",
            "Check that files are formatted. Will not write any changes."
        );
        checkCommand.AddArgument(GetDirectoryOrFileArgument());
        checkCommand.AddValidator(ValidateDirectoryOrFiles());

        return checkCommand;
    }

    public static Command CreatePipeCommand()
    {
        var pipeMultipleFilesCommand = new Command(
            "pipe-files",
            "Keep csharpier running so that multiples files can be piped to it via stdin."
        );

        pipeMultipleFilesCommand.AddValidator(cmd =>
            !Console.IsInputRedirected
                ? "pipe-files may only be used if you pipe stdin to CSharpier"
                : null
        );

        return pipeMultipleFilesCommand;
    }

    public static Command CreateServerCommand()
    {
        var serverCommand = new Command(
            "server",
            "Run CSharpier as a server so that multiple files may be formatted."
        );
        serverCommand.AddOption(
            new Option<int?>(
                ["--server-port"],
                "Specify the port that CSharpier should start on. Defaults to a random unused port."
            )
        );

        return serverCommand;
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

    private static ValidateSymbol<CommandResult> ValidateDirectoryOrFiles()
    {
        return cmd =>
        {
            if (!Console.IsInputRedirected && !cmd.Children.Contains("directoryOrFile"))
            {
                return "directoryOrFile is required when not piping stdin to CSharpier";
            }

            return null;
        };
    }
}
