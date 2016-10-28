namespace Dauber.Azure.Blob.Contracts
{
    public class Blob
    {
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
    }
}