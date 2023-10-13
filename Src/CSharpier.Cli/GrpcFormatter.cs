using Grpc.Core;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using CSharpier.Proto;

namespace CSharpier.Cli;

using System.IO.Abstractions;
using CSharpier.Cli.Options;

public static class GrpcFormatter
{
    public static Task<int> StartServer(
        ConsoleLogger logger,
        string? actualConfigPath,
        CancellationToken cancellationToken
    )
    {
        var port = FindFreePort();
        var server = new Server
        {
            Services =
            {
                CSharpierService.BindService(
                    new CSharpierServiceImplementation(actualConfigPath, logger)
                )
            },
            Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
        };
        server.Start();

        logger.LogInformation("Started on " + port);
        Console.ReadKey();

        return Task.FromResult(0);
    }

    public static int FindFreePort()
    {
        var startPort = 8050;
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
        var ipEndPoint = ipGlobalProperties.GetActiveTcpListeners();

        var usedPorts = ipEndPoint
            .Select(o => o.Port)
            .Concat(tcpConnInfoArray.Select(o => o.LocalEndPoint.Port))
            .ToList();

        for (var i = startPort; i < ushort.MaxValue; i++)
        {
            if (!usedPorts.Contains(i))
            {
                return i;
            }
        }

        throw new InvalidOperationException(
            "Could not find any free TCP port over port " + startPort
        );
    }
}

public class CSharpierServiceImplementation : CSharpierService.CSharpierServiceBase
{
    private readonly string? configPath;
    private readonly IFileSystem fileSystem;
    private readonly ConsoleLogger logger;

    public CSharpierServiceImplementation(string? configPath, ConsoleLogger logger)
    {
        this.configPath = configPath;
        this.logger = logger;
        this.fileSystem = new FileSystem();
    }

    // TODO test csharpier ignore
    // TODO test options file
    public override async Task<FormatFileResult> FormatFile(
        FormatFileDto formatFileDto,
        ServerCallContext context
    )
    {
        try
        {
            var optionsProvider = await OptionsProvider.Create(
                this.fileSystem.Path.GetDirectoryName(formatFileDto.FileName),
                this.configPath,
                this.fileSystem,
                this.logger,
                context.CancellationToken
            );

            if (
                GeneratedCodeUtilities.IsGeneratedCodeFile(formatFileDto.FileName)
                || optionsProvider.IsIgnored(formatFileDto.FileName)
            )
            {
                // TODO should we send back that this is ignored?
                return new FormatFileResult();
            }

            var result = await CSharpFormatter.FormatAsync(
                formatFileDto.FileContents,
                optionsProvider.GetPrinterOptionsFor(formatFileDto.FileName),
                context.CancellationToken
            );

            // TODO what about checking if this actually formatted?
            // could send back any error messages now
            return new FormatFileResult { FormattedFile = result.Code };
        }
        catch (Exception ex)
        {
            // TODO should this return this as an error?
            DebugLogger.Log(ex.ToString());
            return new FormatFileResult();
        }
    }
}
