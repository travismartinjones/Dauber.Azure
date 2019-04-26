using System.Threading.Tasks;

namespace Dauber.Azure.Blob.Contracts
{
    public interface IBlobStore
    {
        Task<BlobCreationResponse> InsertAsync(string contentType, string blobName, byte[] data, bool isPrivate = true);
        Task DeleteAsync(string blobUrl);
        Task<Blob> GetAsync(string blobUrl);
        //http://stackoverflow.com/a/30468172
        Task<string> GetValetKeyUri(string blobUri, int secondsToExpireKey);
    }
}