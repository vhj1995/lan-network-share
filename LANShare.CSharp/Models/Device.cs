using System;

namespace LANShare.CSharp.Models
{
    public class Device
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = "Windows";
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 45455;
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public bool IsOnline { get; set; } = true;

        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? IpAddress : Name;

        public override bool Equals(object? obj)
        {
            if (obj is Device other)
            {
                return string.Equals(IpAddress, other.IpAddress, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (IpAddress ?? string.Empty).GetHashCode(StringComparison.OrdinalIgnoreCase);
        }
    }
}
