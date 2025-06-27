using FCP.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace FCP.Controllers
{
    /// <summary>
    /// Implements the Huffman compression and decompression algorithm.
    /// </summary>
    public class HuffmanAlgorithm : CompressInterface
    {
        /// <summary>
        /// Compresses a byte array using the Huffman algorithm.
        /// </summary>
        public byte[] Compress(byte[] data, CancellationToken token, ManualResetEventSlim pauseEvent)
        {
            // 1. Build the frequency table.
            Dictionary<byte, int> frequencies = BuildFrequencyTable(data);

            if (frequencies.Count <= 1) return null;

            // 2. Build the Huffman Tree from the frequencies.
            Node root = BuildHuffmanTree(frequencies);

            // 3. Build the encoding map (e.g., 'a' -> "01") from the tree.
            Dictionary<byte, string> encodingMap = BuildEncodingMap(root);

            // 4. Encode the data into a string of bits.
            var encodedBitString = new StringBuilder();
            int bytesProcessed = 0;
            const int checkInterval = 4096;
            foreach (byte b in data)
            {
                if (++bytesProcessed % checkInterval == 0)
                {
                    pauseEvent.Wait(token);
                    token.ThrowIfCancellationRequested();
                }

                encodedBitString.Append(encodingMap[b]);
            }

            // Store the exact number of bits before padding.
            int originalBitCount = encodedBitString.Length;

            // 5. Convert the string of bits into a byte array.
            byte[] compressedBytes = BitStringToByteArray(encodedBitString.ToString());

            // 6. Create the compressed file stream in memory.
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true))
                {
                    // Write Header:
                    writer.Write(originalBitCount); // Needed for correct decompression
                    writer.Write(frequencies.Count);
                    foreach (var entry in frequencies)
                    {
                        writer.Write(entry.Key);
                        writer.Write(entry.Value);
                    }
                }

                // Write Data
                memoryStream.Write(compressedBytes, 0, compressedBytes.Length);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Decompresses a byte array that was compressed with this Huffman implementation.
        /// </summary>
        public byte[] Decompress(byte[] compressedData, CancellationToken token, ManualResetEventSlim pauseEvent)
        {
            using (var memoryStream = new MemoryStream(compressedData))
            using (var reader = new BinaryReader(memoryStream, Encoding.UTF8, true))
            {
                // 1. Read Header and Reconstruct Frequency Table
                int originalBitCount = reader.ReadInt32();
                int frequencyCount = reader.ReadInt32();
                var frequencies = new Dictionary<byte, int>();
                for (int i = 0; i < frequencyCount; i++)
                {
                    byte symbol = reader.ReadByte();
                    int frequency = reader.ReadInt32();
                    frequencies[symbol] = frequency;
                }

                // 2. Rebuild the exact same Huffman Tree.
                Node root = BuildHuffmanTree(frequencies);
                Node currentNode = root;

                // 3. Read the compressed data.
                var decompressedBytes = new List<byte>();
                byte[] dataBytes = new byte[memoryStream.Length - memoryStream.Position];
                memoryStream.Read(dataBytes, 0, dataBytes.Length);

                // Reconstruct the bit string manually for accuracy.
                var fullBitString = new StringBuilder(dataBytes.Length * 8);
                foreach (byte b in dataBytes)
                {
                    fullBitString.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
                }

                const int checkInterval = 8192;

                // 4. Decode the data by traversing the tree for each relevant bit.
                for (int i = 0; i < originalBitCount; i++)
                {

                    if (i % checkInterval == 0)
                    {
                        pauseEvent.Wait(token);
                        token.ThrowIfCancellationRequested();
                    }

                    // Traverse left for 0, right for 1.
                    currentNode = fullBitString[i] == '1' ? currentNode.Right : currentNode.Left;

                    // If we've reached a leaf node...
                    if (currentNode.Left == null && currentNode.Right == null)
                    {
                        // ...we have found a symbol. Add it to our result.
                        decompressedBytes.Add(currentNode.Symbol);
                        // Reset traversal back to the root for the next symbol.
                        currentNode = root;
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

        // The core difference from Shannon-Fano is this bottom-up tree building method.
        private Node BuildHuffmanTree(Dictionary<byte, int> frequencies)
        {
            // Create a leaf node for each symbol.
            var nodes = new List<Node>();
            foreach (var entry in frequencies)
            {
                nodes.Add(new Node { Symbol = entry.Key, Frequency = entry.Value });
            }

            // Loop until only one node (the root) remains.
            while (nodes.Count > 1)
            {
                // Sort the list to find the two nodes with the smallest frequencies.
                nodes.Sort();

                Node left = nodes[0];
                Node right = nodes[1];

                // Create a new parent node.
                var parent = new Node
                {
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };

                // Remove the two children from the list.
                nodes.Remove(left);
                nodes.Remove(right);

                // Add the new parent node back into the list.
                nodes.Add(parent);
            }
            // The single remaining node is the root of the tree.
            return nodes.First();
        }

        private Dictionary<byte, string> BuildEncodingMap(Node root)
        {
            var map = new Dictionary<byte, string>();
            BuildEncodingMapRecursive(root, "", map);
            return map;
        }

        private void BuildEncodingMapRecursive(Node node, string currentCode, Dictionary<byte, string> map)
        {
            if (node == null) return;

            // If it's a leaf node, we've found a symbol and its code.
            if (node.Left == null && node.Right == null)
            {
                map[node.Symbol] = string.IsNullOrEmpty(currentCode) ? "0" : currentCode;
                return;
            }

            BuildEncodingMapRecursive(node.Left, currentCode + "0", map);
            BuildEncodingMapRecursive(node.Right, currentCode + "1", map);
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