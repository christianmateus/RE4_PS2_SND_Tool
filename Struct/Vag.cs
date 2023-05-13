namespace RE4_PS2_SND_Tool.Struct
{
    internal class Vag
    {
        public uint Magic = 0x70474156; // VAGp
        public uint Version = 0x03000000; // Version 3
        public uint StartOffset = 0x00; // Always zero
        public uint Length { get; set; }
        public uint Frequency { get; set; }
        public byte[] Reserved1 = new byte[10]; // 10 bytes
        public byte Channels = 0x00;
        public byte Reserved2 = 0x00;
        public byte[] TrackName = new byte[32]; // 32 bytes
    }
}
