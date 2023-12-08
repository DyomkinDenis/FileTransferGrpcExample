using ProtoBuf.Grpc;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace FileTransferContracts
{
    [ServiceContract]
    public interface IFileTransferService
    {
        [OperationContract]
        IAsyncEnumerable<ChunkMsg> Subscribe(FileRequest request, CallContext context = default);
    }

    [DataContract]
    public class ChunkMsg
    {

        [DataMember(Order = 1)]
        public string FileName { get; set; }

        [DataMember(Order = 2)]
        public long FileSize { get; set; }

        [DataMember(Order = 3)]
        public byte[] Chunk { get; set; }
        [DataMember(Order = 4)]
        public int ChunkSize { get; set; }
    }

    [DataContract]
    public class FileRequest
    {
        [DataMember(Order = 1)]
        public string FilePath { get; set; }
    }
}