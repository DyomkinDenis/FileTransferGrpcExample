// See https://aka.ms/new-console-template for more information
using FileTransferContracts;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Grpc.Server;
using ProtoBuf.Meta;

Console.WriteLine("Hello, World!");


//GenerateScheme();

var readTask = Task.Run(async () =>
{
    Console.WriteLine("Press E to exit...");

    char ch = ' ';

    while (ch != 'e')
    {
        Console.WriteLine("Press E to exit...and S to SendRequest");
        ch = char.ToLowerInvariant(Console.ReadKey().KeyChar);
        if (ch == 's')
        {
            await SendRequestForFileContract();
        }
    }

});

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(new[] { "http://localhost:5002", "https://localhost:5003" });

builder.Services.AddCodeFirstGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<FileTransferCodeFirstService.FileTransferCodeFirstService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

static async Task<string> ReceiveAsync(IAsyncEnumerable<ChunkMsg> updates, string tempFile, CancellationToken token)
{
    var finalFile = tempFile;
    long pushedSize = 0;
    try
    {
        await using (var fs = File.OpenWrite(tempFile))
        {
            await foreach (var chunk in updates.WithCancellation(token))
            {
                var totalSize = chunk.FileSize;
                pushedSize += chunk.ChunkSize;
                Console.WriteLine($"{pushedSize}/{totalSize}");

                if (!String.IsNullOrEmpty(chunk.FileName))
                {
                    finalFile = chunk.FileName;
                }

                if (chunk.Chunk.Length == chunk.ChunkSize)
                    fs.Write(chunk.Chunk);
                else
                {
                    fs.Write(chunk.Chunk, 0, chunk.ChunkSize);
                    Console.WriteLine($"final chunk size: {chunk.ChunkSize}");
                }
            }
        }
    }
    catch (RpcException e)
    {
        if (e.StatusCode == StatusCode.Cancelled)
        {
            return finalFile;
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Finished.");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error: {e.Message}");
    }

    return finalFile;
}

static void WaitForExitKey()
{
    Console.WriteLine("Press E to exit...");

    char ch = ' ';

    while (ch != 'e')
    {
        ch = char.ToLowerInvariant(Console.ReadKey().KeyChar);
    }
}

async Task SendRequestForFileContract()
{
    var channel = GrpcChannel.ForAddress("https://localhost:5001/", new GrpcChannelOptions//var _channel = GrpcChannel.ForAddress("https://localhost:7245/", new GrpcChannelOptions
    {
        MaxReceiveMessageSize = 5 * 1024 * 1024, // 5 MB
        MaxSendMessageSize = 5 * 1024 * 1024, // 5 MB
    });

    var client = channel.CreateGrpcService<IFileTransferService>();

    var request = new FileRequest { FilePath = @"c://test-file-under-2gb.avi" };

    var tempFile = $"temp_{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")}.tmp";
    var finalFile = tempFile;

    var updates = client.Subscribe(request);

    var tokenSource = new CancellationTokenSource();
    var task = ReceiveAsync(updates, tempFile, tokenSource.Token);

    finalFile = await task;

    try
    {
        if (finalFile != tempFile)
        {
            File.Move(tempFile, new Random().Next(123) + finalFile);
        }

    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
}


async Task GenerateScheme()
{
    var generator = new SchemaGenerator
    {
        ProtoSyntax = ProtoSyntax.Proto3
    };

    var schema = generator.GetSchema<IFileTransferService>(); // there is also a non-generic overload that takes Type

    using (var writer = new System.IO.StreamWriter("fileTransfer.proto"))
    {
        await writer.WriteAsync(schema);
    }
}