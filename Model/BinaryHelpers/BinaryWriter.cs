using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JeopardyNesTextTool.Model.BinaryHelpers
{
    internal class BinaryWriter
    {
        public string FilePath { get; }
        public BinaryWriter(string filePath, bool isExistingFile)
        {
            FilePath = filePath;
            if (File.Exists(FilePath) == false && isExistingFile)
            {
                throw new FileNotFoundException($"Output file {FilePath} not found");
            }
        }

        public static byte[] NybblesToBytes(byte[] nybbles)
        {
            if (nybbles.Length % 2 != 0)
            {
                nybbles = nybbles.Concat(new byte[1]).ToArray();
            }
            var result = new List<byte>();
            for (var i = 0; i < nybbles.Length; i+=2)
            {
                result.Add((byte)(nybbles[i] << 4 | nybbles[i + 1] & 0xF));
            }
            return result.ToArray();
        }

        public static byte[] BoolsToBytes(List<bool> input)
        {
            if (input.Count % 8 != 0)
            {
                var paddingLength = 8 - (input.Count % 8);
                var paddingBlock = new bool[paddingLength];
                input.AddRange(paddingBlock);
            }
            var ret = new byte[input.Count / 8];
            for (var i = 0; i < input.Count; i += 8)
            {
                var value = 0;
                for (var j = 0; j < 8; j++)
                {
                    if (input[i + j])
                    {
                        value += 1 << (7 - j);
                    }
                }
                ret[i / 8] = (byte)value;
            }
            return ret;
        }

        public static byte[] WordsToBytes(ushort[] words)
        {
            var result = new byte[words.Length * 2];
            for (var i = 0; i < words.Length; i++)
            {
                var low = words[i] & 0xFF;
                var high = words[i] >> 8;
                result[i*2] = (byte)low;
                result[i*2 + 1] = (byte)high;
            }
            return result;
        }

        public void JsonSerialize(List<StructuredTextBlock> textBlocks)
        {
            var jsonSerializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var jsonString = JsonSerializer.Serialize(textBlocks, jsonSerializerOptions);
            File.WriteAllText(FilePath, jsonString);
        }

        /// <summary>
        /// Writes given bytesBlock array to file by given path. File will be created or rewritten.
        /// </summary>
        public void WriteBytesBlock(byte[] bytesBlock)
        {
            using var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write);
            fs.Write(bytesBlock, 0, bytesBlock.Length);
        }

        /// <summary>
        /// Writes byte chunk to ROM at given offset. If targetSize is exceeded, throws Exception. Fills remaining space with fill byte.
        /// </summary>
        public void WriteBytesBlock(byte[] bytesBlock, int offset, int targetSize, byte fillByte)
        {
            if (targetSize < bytesBlock.Length)
            {
                throw new InvalidOperationException(
                    $"Trying to write {bytesBlock.Length} bytesBlock at offset {offset}, but target size is {targetSize} bytesBlock.");
            }

            var fileLength = new FileInfo(FilePath).Length;
            if (offset + targetSize > fileLength)
            {
                throw new InvalidOperationException("Trying to write behind end of file");
            }
            var paddingLength = targetSize - bytesBlock.Length;
            var paddedBlock = new List<byte>(bytesBlock);
            paddedBlock.AddRange(Enumerable.Repeat(fillByte, paddingLength));

            using var stream = File.Open(FilePath, FileMode.Open);
            stream.Seek(offset, SeekOrigin.Begin);
            stream.Write(paddedBlock.ToArray(), 0, targetSize);
        }

        public void WriteWord(ushort word, uint offset)
        {
            using var writer = new System.IO.BinaryWriter(File.Open(FilePath, FileMode.OpenOrCreate));
            writer.Seek((int)offset, SeekOrigin.Begin);
            writer.Write(word);
        }

        public void ExecuteWriteQueue(WriteQueue writeQueue)
        {
            using var stream = File.Open(FilePath, FileMode.Open);
            var fileLength = new FileInfo(FilePath).Length;
            {
                foreach (var element in writeQueue.Elements)
                {
                    
                    if (element.Offset + element.TargetSize > fileLength)
                    {
                        throw new InvalidOperationException($"Trying to write behind end of file at offset{element.Offset}");
                    }
                    var paddingLength = (int)(element.TargetSize - element.Data.Length);
                    var paddedBlock = new List<byte>(element.Data);
                    paddedBlock.AddRange(Enumerable.Repeat(element.FillByte, paddingLength));
                    stream.Seek(element.Offset, SeekOrigin.Begin);
                    stream.Write(paddedBlock.ToArray(), 0, (int)element.TargetSize);
                }
            }
        }
    }

    public class WriteQueueElement
    {
        public uint Offset { get;  }
        public byte[] Data { get;}

        public uint TargetSize { get; }

        public byte FillByte { get; }

        public WriteQueueElement(uint offset, byte[] data, uint targetSize, byte fillByte)
        {
            Offset = offset;
            Data = data;
            TargetSize = targetSize;
            FillByte = fillByte;
        }
    }

    internal class WriteQueue
    {
        public List<WriteQueueElement> Elements { get; set; } = new();

        public void Add(WriteQueueElement element)
        {
            if (element.Data.Length >  element.TargetSize)
            {
                throw new InvalidOperationException(
                    $"Trying to add to write queue {element.Data.Length} bytesBlock at offset {element.Offset}, but target size is {element.TargetSize} bytesBlock.");
            }
            Elements.Add(element);
        }
    }
}
