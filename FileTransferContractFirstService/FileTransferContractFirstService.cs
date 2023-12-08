using FileTransferContracts;
using Google.Protobuf;
using Grpc.Core;


namespace FileTransferContractFirstService
{

    internal class FileTransferContractFirstService : FileTransferService.FileTransferServiceBase
    {
        private readonly FileTransferService.FileTransferServiceClient _client;

        public FileTransferContractFirstService(FileTransferContracts.FileTransferService.FileTransferServiceClient client)
        {
            _client = client;
        }
        public override async Task Subscribe(FileRequest request, IServerStreamWriter<ChunkMsg> responseStream, ServerCallContext context)
        {
            await SendFileFromAnotherService(request, responseStream, context);
            //await SendLocalFile(request, responseStream, context);
        }

        private async Task SendFileFromAnotherService(FileRequest request, IServerStreamWriter<ChunkMsg> responseStream, ServerCallContext context)
        {
            var token = context.CancellationToken;
            var updates = _client.Subscribe(request);

            await foreach (var update in updates.ResponseStream.ReadAllAsync().WithCancellation(token))
            {
                await responseStream.WriteAsync(update);
            }
        }

        private async Task SendLocalFile(FileRequest request, IServerStreamWriter<ChunkMsg> responseStream, ServerCallContext context)
        {
            var token = context.CancellationToken;

            var filePath = request.FilePath;
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);

                var chunk = new ChunkMsg
                {
                    FileName = Path.GetFileName(filePath),
                    FileSize = fileInfo.Length
                };

                var chunkSize = 64 * 1024;

                var fileBytes = File.ReadAllBytes(filePath);
                var fileChunk = new byte[chunkSize];

                var offset = 0;

                while (!token.IsCancellationRequested)
                {
                    if (!(offset < fileBytes.Length))
                    {
                        break;
                    }
                    if (context.CancellationToken.IsCancellationRequested)
                        break;

                    var length = Math.Min(chunkSize, fileBytes.Length - offset);
                    Buffer.BlockCopy(fileBytes, offset, fileChunk, 0, length);

                    offset += length;

                    chunk.ChunkSize = length;
                    chunk.Chunk = ByteString.CopyFrom(fileChunk);

                    await responseStream.WriteAsync(chunk);
                }
            }
        }
    }
}
