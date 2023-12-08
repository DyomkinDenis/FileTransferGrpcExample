using FileTransferContracts;
using ProtoBuf.Grpc;

namespace FileTransferCodeFirstService
{
    internal class FileTransferCodeFirstService : IFileTransferService
    {
        public async IAsyncEnumerable<ChunkMsg> Subscribe(FileRequest request, CallContext context = default)
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
                    chunk.Chunk = fileChunk;

                    yield return new ChunkMsg { Chunk = fileChunk, ChunkSize = length, FileName = chunk.FileName, FileSize = chunk.FileSize };
                }
            }

            Console.WriteLine("yield break");
            yield break;
        }
    }
}
