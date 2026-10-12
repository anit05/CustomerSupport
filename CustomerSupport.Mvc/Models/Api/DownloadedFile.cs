namespace CustomerSupport.Mvc.Models.Api
{
    public sealed record DownloadedFile(byte[] Bytes, string ContentType, string FileName);
}
