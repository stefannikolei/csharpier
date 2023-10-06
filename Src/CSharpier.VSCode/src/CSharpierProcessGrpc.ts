import { ChildProcessWithoutNullStreams, spawn } from "child_process";
import { Logger } from "./Logger";
import { ICSharpierProcess } from "./CSharpierProcess";
import { ProtoGrpcType } from "./proto/csharpier";
import * as grpc from "@grpc/grpc-js";
import * as protoLoader from "@grpc/proto-loader";
import { FormatFileResult } from "./proto/proto/FormatFileResult";
import * as vscode from "vscode";
import { TaskDefinition, TaskRevealKind } from "vscode";
import { CSharpierServiceClient } from "./proto/proto/CSharpierService";
import * as path from "path";

export class CSharpierProcessGrpc implements ICSharpierProcess {
    private logger: Logger;
    private client: CSharpierServiceClient;
    private clientIsReady = false;

    constructor(logger: Logger, csharpierPath: string, workingDirectory: string) {
        this.logger = logger;

        const definition: TaskDefinition = {
            type: "string",
        };

        // TODO what about linux? and why is this needed in here?
        // maybe because this is using ProcessExecution vs just a process?
        csharpierPath += ".exe";

        // TODO need to get the port back from this somehow
        let task = new vscode.Task(
            definition,
            vscode.TaskScope.Workspace,
            "csharpier",
            "csharpier",
            new vscode.ProcessExecution(csharpierPath, ["--grpc"], {
                cwd: workingDirectory,
                env: { ...process.env, DOTNET_NOLOGO: "1" },
            }),
        );

        task.presentationOptions.reveal = TaskRevealKind.Never;

        vscode.tasks.executeTask(task);

        const host = "0.0.0.0:50052";
        const protoFile = require("./proto/csharpier.proto");
        const packageDefinition = protoLoader.loadSync(path.join(__dirname, protoFile));
        const proto = grpc.loadPackageDefinition(packageDefinition) as unknown as ProtoGrpcType;

        this.client = new proto.proto.CSharpierService(host, grpc.credentials.createInsecure());
        const deadline = new Date();
        deadline.setSeconds(deadline.getSeconds() + 5);
        this.client.waitForReady(deadline, (error?: Error) => {
            if (error) {
                this.logger.info(`Client connect error: ${error.message}`);
                this.clientIsReady = true;
            } else {
                this.clientIsReady = true;
                this.logger.debug("Warm CSharpier with initial format");
                // warm by formatting a file twice, the 3rd time is when it gets really fast
                this.formatFile("public class ClassName { }", "Test.cs").then(() => {
                    this.formatFile("public class ClassName { }", "Test.cs");
                });
            }
        });
    }

    formatFile(content: string, filePath: string): Promise<string> {
        const sleep = (delay: number) => new Promise(resolve => setTimeout(resolve, delay));

        return new Promise<string>(async resolve => {
            // TODO this should have a limit for how long it sleeps
            while (!this.clientIsReady) {
                await sleep(10);
            }

            this.client.formatFile(
                {
                    FileName: filePath,
                    FileContents: content,
                },
                (error?: grpc.ServiceError | null, serverMessage?: FormatFileResult) => {
                    if (error) {
                        this.logger.error(error.message);
                        resolve("");
                    } else if (serverMessage) {
                        this.logger.debug(serverMessage.FormattedFile);
                        resolve(serverMessage.FormattedFile!);
                    }
                },
            );
        });
    }

    dispose() {
        this.client.close();
        // TODO also kill task?
    }
}
