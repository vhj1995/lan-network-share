using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LANShare.CSharp.Network
{
    public class BroadcastPayload
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("os")]
        public string OS { get; set; } = "Windows";

        [JsonPropertyName("ip")]
        public string Ip { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public int Port { get; set; } = 45455;
    }

    public class FileInfoHeader
    {
        [JsonPropertyName("relativePath")]
        public string RelativePath { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class TransferHeader
    {
        [JsonPropertyName("transferId")]
        public string TransferId { get; set; } = string.Empty;

        [JsonPropertyName("senderName")]
        public string SenderName { get; set; } = string.Empty;

        [JsonPropertyName("senderOs")]
        public string SenderOs { get; set; } = string.Empty;

        [JsonPropertyName("senderIp")]
        public string SenderIp { get; set; } = string.Empty;

        [JsonPropertyName("folderName")]
        public string FolderName { get; set; } = string.Empty;

        [JsonPropertyName("totalSize")]
        public long TotalSize { get; set; }

        [JsonPropertyName("files")]
        public List<FileInfoHeader> Files { get; set; } = new List<FileInfoHeader>();
    }
}
