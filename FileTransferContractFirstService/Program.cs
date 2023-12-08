// See https://aka.ms/new-console-template for more information
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");

var builder = WebApplication.CreateBuilder(args);
var channel = GrpcChannel.ForAddress("https://localhost:5003/", new GrpcChannelOptions//var _channel = GrpcChannel.ForAddress("https://localhost:7245/", new GrpcChannelOptions
{
    MaxReceiveMessageSize = 5 * 1024 * 1024, // 5 MB
    MaxSendMessageSize = 5 * 1024 * 1024, // 5 MB
});


var client = new FileTransferContracts.FileTransferService.FileTransferServiceClient(channel);

builder.Services.AddSingleton(client);


builder.Services.AddGrpc();

builder.WebHost.UseUrls(new[] { "http://localhost:5000", "https://localhost:5001" });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<FileTransferContractFirstService.FileTransferContractFirstService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
