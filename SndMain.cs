using RE4_PS2_SND_Tool.Struct;
using System;
using System.IO;

namespace RE4_PS2_SND_Tool
{
    internal class SndMain
    {
        SND SND = new SND();
        Vag VAG = new Vag();
        string filepath = "";

        public SndMain(string fileName, string command)
        {
            this.filepath = fileName;
            if (command == "extract")
            {
                Extract(fileName);
            }
            else
            {
                Repack(fileName);
            }
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
                SND.GroupCount = 2;

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

            // Reads second group on loop if it exists
            switch (groupNumber)
            {
                case 0:
                    groupNumber = (int)SND.HeaderOffset_1;
                    break;
                case 1:
                    groupNumber = (int)SND.HeaderOffset_2;
                    break;
                default:
                    break;
            }

            // Obtain more info to get to the Vagi chunk
            br.BaseStream.Position = groupNumber;
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
            SND.AudioCount = br.ReadUInt32() + 1;
            br.BaseStream.Position += 4;

            SND.AudioOffsets = new uint[SND.AudioCount];
            SND.AudioLengths = new uint[SND.AudioCount];
            SND.AudioLoop = new uint[SND.AudioCount];
            SND.AudioFrequencies = new uint[SND.AudioCount];

            // Get all audios parameters
            for (int i = 0; i < SND.AudioCount; i++)
            {
                SND.AudioOffsets[i] = br.ReadUInt32();
                SND.AudioLengths[i] = br.ReadUInt32();
                SND.AudioLoop[i] = br.ReadUInt32();
                SND.AudioFrequencies[i] = br.ReadUInt32();
            }
            br.Close();
        }

        public int GetGroupCount()
        {
            BinaryReader br = new BinaryReader(File.OpenRead(filepath));
            br.BaseStream.Position = 0x84;
            if (br.ReadUInt32() > 0)
            {
                SND.GroupCount = 2;
            }
            else
            {
                SND.GroupCount = 1;
            }
            br.Close();
            return SND.GroupCount;
        }

        public void Extract(string fileName)
        {
            string folderName = Path.GetFileNameWithoutExtension(fileName);
            SND.GroupCount = GetGroupCount();

            // Create directory
            if (!Directory.Exists($"{folderName}"))
            {
                Directory.CreateDirectory($"{folderName}");
            }

            // Creates .txt file for easier repacking later
            StreamWriter txt = File.CreateText($"{folderName}.txt");
            txt.WriteLine("GroupCount = " + GetGroupCount());

            // Get audios and extract
            for (int group = 0; group < SND.GroupCount; group++)
            {
                GetHeaderInfo(fileName, group);

                txt.WriteLine("FileCount = " + SND.AudioCount);

                BinaryReader br = new BinaryReader(File.OpenRead(fileName));
                for (int i = 0; i < SND.AudioCount; i++)
                {
                    if (group == 0)
                    {
                        br.BaseStream.Position = SND.VagChunkOffset_1 + SND.AudioOffsets[i];
                    }
                    else
                    {
                        br.BaseStream.Position = SND.VagChunkOffset_2 + SND.AudioOffsets[i];
                    }
                    SND.AudioBytes = br.ReadBytes((int)SND.AudioLengths[i]);
                    VAG.Length = SND.AudioLengths[i];
                    VAG.Frequency = SND.AudioFrequencies[i];

                    txt.WriteLine($"File_{group}_{i} = " + $"{folderName}/{folderName}_{group}_{i}.vag");

                    // Begin writing the .vag files
                    BinaryWriter bw = new BinaryWriter(File.Create($"{folderName}/{folderName}_{group}_{i}.vag"));
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
            txt.Close();
        }

        public void Repack(string filename)
        {
            string folderName = Path.GetFileNameWithoutExtension(filename);

            int groupCount = GetGroupCount();
            uint vagChunkOffset1 = 0;
            uint vagChunkOffset2 = 0;
            uint vagiOffset1 = 0;
            uint vagiOffset2 = 0;

            for (int i = 0; i < groupCount; i++)
            {
                GetHeaderInfo(filename, i);
                if (i == 0)
                {
                    vagChunkOffset1 = SND.VagChunkOffset_1;
                    vagiOffset1 = SND.VagiOffset;
                }
                else
                {
                    vagChunkOffset2 = SND.VagChunkOffset_2;
                    vagiOffset2 = SND.VagiOffset;
                }
            }
            // Get headers
            BinaryReader br = new BinaryReader(File.OpenRead(filename));
            byte[] header1 = br.ReadBytes((int)vagChunkOffset1);
            byte[] header2 = null;
            if (groupCount == 2)
            {
                br.BaseStream.Position = SND.MetaDataOffset_2;
                header2 = br.ReadBytes((int)(vagChunkOffset2 - br.BaseStream.Position));
            }
            br.Close();

            // Read .idx file
            StreamReader txt = new StreamReader(folderName + ".txt", System.Text.Encoding.UTF8);
            groupCount = Convert.ToInt16(txt.ReadLine().Substring(13));

            // Overwrite snd file
            BinaryWriter bw = new BinaryWriter(File.Create(filename));
            bw.Write(header1);

            // Group 1
            int fileCount = Convert.ToInt16(txt.ReadLine().Substring(12));
            int acumulatorOffset = 0;
            SND.AudioLengths = new uint[fileCount];
            SND.AudioFrequencies = new uint[fileCount];

            for (int sound = 0; sound < fileCount; sound++)
            {
                string line = txt.ReadLine();
                string vagName = line.Substring(line.IndexOf(folderName));

                BinaryReader brVag = new BinaryReader(File.OpenRead(vagName));
                brVag.BaseStream.Position = 0x10;
                SND.AudioFrequencies[sound] = SwapEndianness(brVag.ReadUInt32());

                // Move to audio start offset
                brVag.BaseStream.Position = 0x40;
                SND.AudioBytes = brVag.ReadBytes((int)(brVag.BaseStream.Length - brVag.BaseStream.Position));
                brVag.Close();

                SND.AudioLengths[sound] = (uint)SND.AudioBytes.Length;
                bw.Write(SND.AudioBytes);
            }

            // Update parameters (offset, length and frequency)
            bw.BaseStream.Position = vagiOffset1 + 0x10;
            for (int param = 0; param < fileCount; param++)
            {
                bw.Write(acumulatorOffset);
                bw.Write(SND.AudioLengths[param]);
                bw.Write((uint)0x00);
                bw.Write(SND.AudioFrequencies[param]);
                acumulatorOffset += (int)SND.AudioLengths[param];
            }

            // Update header offset and length
            bw.BaseStream.Position = 0x64;
            bw.Write((uint)bw.BaseStream.Length - SND.VagChunkOffset_1);
            SND.VagChunkLength_1 = (uint)(bw.BaseStream.Length - SND.VagChunkOffset_1);
            bw.BaseStream.Position = bw.BaseStream.Length;

            // ===========================
            // GROUP 2
            // ===========================
            // Do the same for group 2
            acumulatorOffset = 0;

            if (groupCount == 2)
            {
                fileCount = Convert.ToInt16(txt.ReadLine().Substring(12));
                SND.AudioLengths = new uint[fileCount];
                SND.AudioFrequencies = new uint[fileCount];
                bw.Write(header2);

                for (int sound = 0; sound < fileCount; sound++)
                {
                    string line = txt.ReadLine();
                    string vagName = line.Substring(line.IndexOf(folderName));
                    Console.WriteLine("Writing: " + vagName);

                    BinaryReader brVag = new BinaryReader(File.OpenRead(vagName));
                    brVag.BaseStream.Position = 0x10;
                    SND.AudioFrequencies[sound] = SwapEndianness(brVag.ReadUInt32());

                    // Move to audio start offset
                    brVag.BaseStream.Position = 0x40;
                    SND.AudioBytes = brVag.ReadBytes((int)(brVag.BaseStream.Length - brVag.BaseStream.Position));
                    brVag.Close();

                    SND.AudioLengths[sound] = (uint)SND.AudioBytes.Length;
                    bw.Write(SND.AudioBytes);
                }

                // Update header offsets
                bw.BaseStream.Position = 0x8C;
                bw.Write(SND.VagChunkLength_1 + SND.VagChunkOffset_1);

                bw.BaseStream.Position = 0xAC;
                string verifyLineBreak = SND.MetaDataLength_2.ToString("X");
                uint padding = 0;

                // 0x0F block alignment required, sometimes the metadata ends before the row is complete
                switch (verifyLineBreak.Substring(verifyLineBreak.Length - 1))
                {
                    case "4":
                        padding += 12;
                        break;
                    case "8":
                        padding += 8;
                        break;
                    case "C":
                        padding += 4;
                        break;
                    default:
                        break;
                }
                bw.Write(SND.MetaDataLength_2 + SND.MetaDataOffset_2 + padding);

                // Vag chunk offset
                bw.BaseStream.Position = 0xCC;
                bw.Write(SND.HeaderLength_2 + SND.HeaderOffset_2);

                // Vag chunk length
                bw.BaseStream.Position = 0xC4;
                bw.Write((uint)bw.BaseStream.Length - SND.VagChunkOffset_2);

                // Update parameters (offset, length and frequency)
                bw.BaseStream.Position = vagiOffset2 + 0x10;
                for (int param = 0; param < fileCount; param++)
                {
                    bw.Write(acumulatorOffset);
                    bw.Write(SND.AudioLengths[param]);
                    bw.Write((uint)0x00);
                    bw.Write(SND.AudioFrequencies[param]);
                    acumulatorOffset += (int)SND.AudioLengths[param];
                }
            }
            txt.Close();
            bw.Close();
            UpdateHeaderOffsets();
        }

        public static uint SwapEndianness(uint value)
        {
            var b1 = (value >> 0) & 0xff;
            var b2 = (value >> 8) & 0xff;
            var b3 = (value >> 16) & 0xff;
            var b4 = (value >> 24) & 0xff;

            return (uint)(b1 << 24 | b2 << 16 | b3 << 8 | b4 << 0);
        }

        // Update Head offset (which sometimes have padding on rXXX.snd files)
        public void UpdateHeaderOffsets()
        {
            BinaryReader br = new BinaryReader(File.OpenRead(filepath));
            uint newHeadOffset = 0;
            uint newVagOffset = 0;

            br.BaseStream.Position = 0xAC;
            br.BaseStream.Position = br.ReadUInt32();

            if (br.ReadUInt32() != 1684104520) // this is the Head magic name in uint32 value
            {
                br.BaseStream.Position = 0xAC;
                br.BaseStream.Position = br.ReadUInt32() - 0x10;
                if (br.ReadUInt32() == 1684104520)
                {
                    br.BaseStream.Position -= 4;
                    newHeadOffset = (uint)br.BaseStream.Position;

                    // If there's padding, add more 0x10 to vag start offset as well
                    br.BaseStream.Position = 0xCC;
                    newVagOffset = br.ReadUInt32();
                }
                else
                {
                    br.BaseStream.Position = 0xAC;
                    br.BaseStream.Position = br.ReadUInt32() + 0x10;
                    newHeadOffset = (uint)br.BaseStream.Position;

                    // If there's padding, add more 0x10 to vag start offset as well
                    br.BaseStream.Position = 0xCC;
                    newVagOffset = br.ReadUInt32() + 0x10;
                }
            }
            br.Close();

            BinaryWriter bw = new BinaryWriter(File.OpenWrite(filepath));
            if (newHeadOffset > 0)
            {
                bw.BaseStream.Position = 0xAC;
                bw.Write(newHeadOffset);
                bw.BaseStream.Position = 0xCC;
                bw.Write(newVagOffset);
            }
            bw.Close();
        }
    }
}
