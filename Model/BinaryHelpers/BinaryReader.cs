using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JeopardyNesTextTool.Model.BinaryHelpers
{
    internal class BinaryReader
    {
        public const ushort TextBankOffset = 0x10;
        public string FilePath{ get; }


        public BinaryReader(string filePath)
        {
            FilePath = filePath;
            if (File.Exists(FilePath) == false)
            {
                throw new FileNotFoundException("Input file not found");
            }
        }
        public IEnumerable<ushort> ReadWordsBlock(uint tableOffset, int pointersCount)
        {

            var questionBlocksPointerBytes =ReadBytesBlock(tableOffset, pointersCount * 2);
            var questionBlocksPointersWords = new ushort[pointersCount];
            Buffer.BlockCopy(questionBlocksPointerBytes, 0, questionBlocksPointersWords, 0, pointersCount * 2);
            return questionBlocksPointersWords.Select(offset => (ushort)(offset + TextBankOffset));
        }

        public ushort ReadWord(uint offset)
        {
            using var fileBinaryReader = new System.IO.BinaryReader(File.OpenRead(FilePath));
            {
                fileBinaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                return fileBinaryReader.ReadUInt16();
            }
        }

        public byte[] ReadBytesBlock(uint offset, int size)
        {
            using var fileBinaryReader = new System.IO.BinaryReader(File.OpenRead(FilePath));
            {
                fileBinaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                return fileBinaryReader.ReadBytes(size);
            }
        }

        public byte[] ReadNybblesBlock(uint offset, int sizeInNybbles)
        {
            var sizeInBytes = sizeInNybbles / 2;
            using var fileBinaryReader = new System.IO.BinaryReader(File.OpenRead(FilePath));
            {
                fileBinaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                var bytesBlock = fileBinaryReader.ReadBytes(sizeInBytes);
                return bytesBlock.SelectMany(SplitByteToNybbles).ToArray();

            }
        }

        private static byte[] SplitByteToNybbles(byte intputByte)
        {
            return new[] { (byte)(intputByte >> 4), (byte)(intputByte & 0xF) };
        }

        public IEnumerable<bool> ReadBitsBlock(uint offset)
        {
            const int blockSize = 0x4000;
            var bytes = ReadBytesBlock(offset, blockSize);
            return bytes.SelectMany(GetBits);
        }

        private static IEnumerable<bool> GetBits(byte b)
        {
            for (var i = 0; i < 8; i++)
            {
                yield return (b & 0x80) != 0;
                b *= 2;
            }
        }

        public List<StructuredTextBlock> JsonDeserialize()
        {
            var jsonSerializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var jsonString = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<StructuredTextBlock>>(jsonString, jsonSerializerOptions);
        }

    }
}