namespace Bootloader.Domain
{
    public class NewVersion
    {
        public string VersionNumber { get; set; } = string.Empty;
        public byte[] Data { get; set; } = new byte[0];
    }
}
