using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Spike1
{
    class WavReader
    {
        private char[] chunkId;
        public uint chunkSize { get; private set; } = 0;
        public char[] format { get; private set; }
        public char[] subchunk1Id { get; private set; }
        public uint subchunk1Size { get; private set; }
        public ushort audioFormat { get; private set; }
        public ushort numChannels { get; private set; }
        public uint sampleRates { get; private set; } 
        public uint byteRate { get; private set; }
        public ushort blockAlign { get; private set; }
        public ushort bitsPerSample { get; private set; }
        public char[] subchunk2Id { get; private set; }
        public uint subchunk2Size { get; private set; }

        byte[] rawdata = new byte[1];
        public WavReader(string filename)
        {
            System.IO.FileStream file = System.IO.File.OpenRead(filename);
            
            byte[] arr = new byte[4];
            file.Read(arr, 0, 4);
            chunkId = Encoding.UTF8.GetString(arr).ToCharArray();

            arr = new byte[4];
            file.Read(arr, 0, 4);
            chunkSize = BitConverter.ToUInt32(arr, 0);

            arr = new byte[4];
            file.Read(arr, 0, 4);
            format = Encoding.UTF8.GetString(arr).ToCharArray();

            arr = new byte[4];
            file.Read(arr, 0, 4);
            subchunk1Id = Encoding.UTF8.GetString(arr).ToCharArray();

            arr = new byte[4];
            file.Read(arr, 0, 4);
            subchunk1Size = BitConverter.ToUInt32(arr, 0);

            arr = new byte[2];
            file.Read(arr, 0, 2);
            audioFormat = BitConverter.ToUInt16(arr, 0);

            arr = new byte[2];
            file.Read(arr, 0, 2);
            numChannels = BitConverter.ToUInt16(arr, 0);

            arr = new byte[4];
            file.Read(arr, 0, 4);
            sampleRates = BitConverter.ToUInt32(arr, 0);

            arr = new byte[4];
            file.Read(arr, 0,  4);
            byteRate = BitConverter.ToUInt32(arr, 0);

            arr = new byte[2];
            file.Read(arr, 0, 2);
            blockAlign = BitConverter.ToUInt16(arr, 0);

            arr = new byte[2];
            file.Read(arr, 0, 2);
            bitsPerSample = BitConverter.ToUInt16(arr, 0);

            arr = new byte[4];
            file.Read(arr, 0, 4);
            subchunk2Id = Encoding.UTF8.GetString(arr).ToCharArray();

            arr = new byte[4];
            file.Read(arr, 0, 4);
            subchunk2Size = BitConverter.ToUInt32(arr, 0);

            arr = new byte[file.Length - 44];
            file.Read(arr, 0, (int)subchunk2Size);
            rawdata = arr;

            file.Close();
        }
        
        public Int16[] GetData()
        {
            List<Int16> data = new List<Int16>();
            for (int i = 0; i<rawdata.Length; i++)
            {
                if (blockAlign ==2)
                {
                    byte[] sample = { rawdata[i], rawdata[i + 1] };
                    data.Add(BitConverter.ToInt16(sample));
                    i++;
                }
            }
            return data.ToArray();
        }
    }
}
