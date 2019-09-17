using System.Threading.Tasks;

namespace Dauber.Azure.Blob.Contracts
{
    public interface IBlobStore
    {
        Task<BlobCreationResponse> InsertAsync(string contentType, string blobName, byte[] data, bool isPrivate = true);
        Task DeleteAsync(string blobUrl);
        Task DeleteAsync(string blobUrl, bool isPrivate);
        Task<Blob> GetAsync(string blobUrl);
        Task<Blob> GetAsync(string blobUrl, bool isPrivate);
        //http://stackoverflow.com/a/30468172
        Task<string> GetValetKeyUri(string blobUri, int secondsToExpireKey);
    }
}