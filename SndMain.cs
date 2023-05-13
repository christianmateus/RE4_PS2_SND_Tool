using RE4_PS2_SND_Tool.Struct;
using System;
using System.IO;

namespace RE4_PS2_SND_Tool
{
    internal class SndMain
    {
        SND SND = new SND();
        Vag VAG = new Vag();

        public SndMain(string fileName)
        {
            Extract(fileName);
        }

        public void GetHeaderInfo(string fileName, int groupNumber)
        {
            BinaryReader br = new BinaryReader(File.OpenRead(fileName));
            br.BaseStream.Position = 0x24;
            SND.MetaDataLength_1 = br.ReadUInt32();
            br.BaseStream.Position = 0x2C;
            SND.MetaDataOffset_1 = br.ReadUInt32();

            br.BaseStream.Position = 0x44;
            SND.HeaderLength_1 = br.ReadUInt32();
            br.BaseStream.Position = 0x4C;
            SND.HeaderOffset_1 = br.ReadUInt32();

            br.BaseStream.Position = 0x64;
            SND.VagChunkLength_1 = br.ReadUInt32();
            br.BaseStream.Position = 0x6C;
            SND.VagChunkOffset_1 = br.ReadUInt32();

            // Check if group 2 exists
            br.BaseStream.Position = 0x84;
            if (br.ReadUInt32() > 0)
            {
                br.BaseStream.Position = 0x84;
                SND.MetaDataLength_2 = br.ReadUInt32();
                br.BaseStream.Position = 0x8C;
                SND.MetaDataOffset_2 = br.ReadUInt32();

                br.BaseStream.Position = 0xA4;
                SND.HeaderLength_2 = br.ReadUInt32();
                br.BaseStream.Position = 0xAC;
                SND.HeaderOffset_2 = br.ReadUInt32();

                br.BaseStream.Position = 0xC4;
                SND.VagChunkLength_2 = br.ReadUInt32();
                br.BaseStream.Position = 0xCC;
                SND.VagChunkOffset_2 = br.ReadUInt32();
            }

            // Obtain more info to get to the Vagi chunk
            br.BaseStream.Position = SND.HeaderOffset_1;
            SND.HEADOffset = (uint)br.BaseStream.Position;
            br.BaseStream.Position += 4;
            SND.HEADLength = br.ReadUInt32();

            br.BaseStream.Position = SND.HEADOffset + SND.HEADLength;
            SND.PROGOffset = (uint)br.BaseStream.Position;
            br.BaseStream.Position += 4;
            SND.PROGLength = br.ReadUInt32();

            br.BaseStream.Position = SND.PROGOffset + SND.PROGLength;
            SND.SMPLOffset = (uint)br.BaseStream.Position;
            br.BaseStream.Position += 4;
            SND.SMPLLength = br.ReadUInt32();

            br.BaseStream.Position = SND.SMPLOffset + SND.SMPLLength;
            SND.VagiOffset = (uint)br.BaseStream.Position;
            br.BaseStream.Position += 4;
            SND.VagiLength = br.ReadUInt32();
            SND.AudioCount = br.ReadUInt32();
            br.BaseStream.Position += 4;

            SND.AudioOffsets = new uint[SND.AudioCount];
            SND.AudioLengths = new uint[SND.AudioCount];
            SND.AudioFrequencies = new uint[SND.AudioCount];
            Console.WriteLine(SND.AudioCount.ToString("X"));

            // Get all audios parameters
            for (int i = 0; i < SND.AudioCount; i++)
            {
                SND.AudioOffsets[i] = br.ReadUInt32();
                SND.AudioLengths[i] = br.ReadUInt32();
                br.BaseStream.Position += 4;
                SND.AudioFrequencies[i] = br.ReadUInt32();
            }

            br.Close();
        }

        public void Extract(string fileName)
        {
            GetHeaderInfo(fileName, 1);

            // Create directory
            if (!Directory.Exists("Audios"))
            {
                Directory.CreateDirectory("Audios");
            }

            // Get audios and extract
            BinaryReader br = new BinaryReader(File.OpenRead(fileName));
            for (int i = 0; i < SND.AudioCount; i++)
            {
                br.BaseStream.Position = SND.VagChunkOffset_1 + SND.AudioOffsets[i];
                SND.AudioBytes = br.ReadBytes((int)SND.AudioLengths[i]);
                VAG.Length = SND.AudioLengths[i];
                VAG.Frequency = SND.AudioFrequencies[i];

                BinaryWriter bw = new BinaryWriter(File.Create($"Audios/{i}.vag"));
                bw.Write(VAG.Magic);
                bw.Write(VAG.Version);
                bw.Write(VAG.StartOffset);

                // Vag header uses Big Endian values, so this is needed
                for (int j = 0; j < 4; j++)
                {
                    byte[] arr = BitConverter.GetBytes(VAG.Length);
                    bw.Write(arr[3 - j]);
                }

                for (int j = 0; j < 4; j++)
                {
                    byte[] arr = BitConverter.GetBytes(VAG.Frequency);
                    bw.Write(arr[3 - j]);
                }

                bw.Write(VAG.Reserved1);
                bw.Write(VAG.Channels);
                bw.Write(VAG.Reserved2);
                bw.Write(VAG.TrackName);
                bw.Write(SND.AudioBytes);
                bw.Close();
            }
            br.Close();
        }
    }
}
