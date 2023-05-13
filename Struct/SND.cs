namespace RE4_PS2_SND_Tool
{
    internal class SND
    {
        // Group 1 (present in all .snd files)
        public uint MetaDataLength_1 { get; set; }
        public uint MetaDataOffset_1 { get; set; }
        public uint HeaderLength_1 { get; set; }
        public uint HeaderOffset_1 { get; set; }
        public uint VagChunkLength_1 { get; set; }
        public uint VagChunkOffset_1 { get; set; }
        public uint HEADOffset { get; set; }
        public uint HEADLength { get; set; }
        public uint PROGOffset { get; set; }
        public uint PROGLength { get; set; }
        public uint SMPLOffset { get; set; }
        public uint SMPLLength { get; set; }

        // Group 2 (used in room audios rXXX.snd)
        public uint MetaDataLength_2 { get; set; }
        public uint MetaDataOffset_2 { get; set; }
        public uint HeaderLength_2 { get; set; }
        public uint HeaderOffset_2 { get; set; }
        public uint VagChunkLength_2 { get; set; }
        public uint VagChunkOffset_2 { get; set; }

        // Audio parameters
        public uint VagiOffset { get; set; }
        public uint VagiLength { get; set; }
        public uint AudioCount { get; set; }
        public uint[] AudioOffsets { get; set; }
        public uint[] AudioLengths { get; set; }
        public uint[] AudioFrequencies { get; set; }
        public byte[] AudioBytes { get; set; }
        public byte[] AllAudioBytes { get; set; }
    }
}
