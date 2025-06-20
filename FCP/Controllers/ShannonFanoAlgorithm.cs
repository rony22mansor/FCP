using FCP.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace FCP.Controllers
{
    /// <summary>
    /// Implements the Shannon-Fano compression and decompression algorithm.
    /// </summary>
    public class ShannonFanoAlgorithm : CompressInterface
    {
        // The main public method for compression.
        public byte[] Compress(byte[] data)
        {
            var frequencies = BuildFrequencyTable(data);
            if (frequencies.Count <= 1) return null;

            var encodingMap = BuildEncodingMap(frequencies);

            var encodedBitString = new StringBuilder();
            foreach (byte b in data)
            {
                encodedBitString.Append(encodingMap[b]);
            }

            int originalBitCount = encodedBitString.Length;

            byte[] compressedBytes = BitStringToByteArray(encodedBitString.ToString());

            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true))
                {
                    writer.Write(originalBitCount);
                    writer.Write(frequencies.Count);
                    foreach (var entry in frequencies)
                    {
                        writer.Write(entry.Key);
                        writer.Write(entry.Value);
                    }
                }

                memoryStream.Write(compressedBytes, 0, compressedBytes.Length);
                return memoryStream.ToArray();
            }
        }

        // The main public method for decompression.
        public byte[] Decompress(byte[] compressedData)
        {
            using (var memoryStream = new MemoryStream(compressedData))
            using (var reader = new BinaryReader(memoryStream, Encoding.UTF8, true))
            {
                // 1. Read Header & Rebuild Frequency Table
                int originalBitCount = reader.ReadInt32();
                int frequencyCount = reader.ReadInt32();
                var frequencies = new Dictionary<byte, int>();
                for (int i = 0; i < frequencyCount; i++)
                {
                    byte symbol = reader.ReadByte();
                    int frequency = reader.ReadInt32();
                    frequencies[symbol] = frequency;
                }

                // 2. Rebuild the encoding map and then invert it for decoding.
                var encodingMap = BuildEncodingMap(frequencies);
                var decodingMap = encodingMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

                // 3. Read the rest of the stream
                var decompressedBytes = new List<byte>();
                byte[] dataBytes = new byte[memoryStream.Length - memoryStream.Position];
                memoryStream.Read(dataBytes, 0, dataBytes.Length);

                // **THE FIX IS HERE: Reconstruct the bit string manually instead of using BitArray**
                var fullBitString = new StringBuilder(dataBytes.Length * 8);
                foreach (byte b in dataBytes)
                {
                    fullBitString.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
                }

                // Take only the bits from the original data, ignoring the padding.
                string relevantBits = fullBitString.ToString().Substring(0, originalBitCount);

                // 4. Decode the data by matching bit sequences to the decoding map.
                var currentCode = new StringBuilder();
                foreach (char bit in relevantBits)
                {
                    currentCode.Append(bit);
                    if (decodingMap.ContainsKey(currentCode.ToString()))
                    {
                        decompressedBytes.Add(decodingMap[currentCode.ToString()]);
                        currentCode.Clear();
                    }
                }

                return decompressedBytes.ToArray();
            }
        }

        private Dictionary<byte, int> BuildFrequencyTable(byte[] data)
        {
            var frequencies = new Dictionary<byte, int>();
            foreach (byte b in data)
            {
                if (!frequencies.ContainsKey(b))
                {
                    frequencies[b] = 0;
                }
                frequencies[b]++;
            }
            return frequencies;
        }

        private Dictionary<byte, string> BuildEncodingMap(Dictionary<byte, int> frequencies)
        {
            var sortedFrequencies = frequencies.ToList();
            sortedFrequencies.Sort((pair1, pair2) =>
            {
                int frequencyCompare = pair2.Value.CompareTo(pair1.Value);
                if (frequencyCompare == 0)
                {
                    return pair1.Key.CompareTo(pair2.Key);
                }
                return frequencyCompare;
            });

            var encodingMap = new Dictionary<byte, string>();
            ShannonFanoRecursive(sortedFrequencies, "", encodingMap);
            return encodingMap;
        }

        private void ShannonFanoRecursive(List<KeyValuePair<byte, int>> symbols, string currentCode, Dictionary<byte, string> map)
        {
            if (symbols.Count == 0) return;

            if (symbols.Count == 1)
            {
                map[symbols[0].Key] = string.IsNullOrEmpty(currentCode) ? "0" : currentCode;
                return;
            }

            int splitIndex = FindSplitPoint(symbols);

            var leftPart = symbols.GetRange(0, splitIndex + 1);
            ShannonFanoRecursive(leftPart, currentCode + "0", map);

            var rightPart = symbols.GetRange(splitIndex + 1, symbols.Count - (splitIndex + 1));
            ShannonFanoRecursive(rightPart, currentCode + "1", map);
        }

        private int FindSplitPoint(List<KeyValuePair<byte, int>> symbols)
        {
            long totalFrequency = symbols.Sum(s => (long)s.Value);
            long leftSum = 0;
            int bestSplitIndex = 0;
            long minDifference = totalFrequency;

            for (int i = 0; i < symbols.Count - 1; i++)
            {
                leftSum += symbols[i].Value;
                long rightSum = totalFrequency - leftSum;
                long difference = Math.Abs(leftSum - rightSum);

                if (difference < minDifference)
                {
                    minDifference = difference;
                    bestSplitIndex = i;
                }
            }
            return bestSplitIndex;
        }

        private byte[] BitStringToByteArray(string bitString)
        {
            int padding = 8 - bitString.Length % 8;
            if (padding != 8)
            {
                bitString = bitString.PadRight(bitString.Length + padding, '0');
            }

            var byteArray = new byte[bitString.Length / 8];
            for (int i = 0; i < byteArray.Length; i++)
            {
                byteArray[i] = Convert.ToByte(bitString.Substring(i * 8, 8), 2);
            }
            return byteArray;
        }
    }
}
