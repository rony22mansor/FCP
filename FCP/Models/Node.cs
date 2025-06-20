using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCP.Models
{
    /// <summary>
    /// Represents a node in the Huffman Tree.
    /// Can be either a leaf node (with a symbol) or an internal node (with children).
    /// </summary>
    public class Node : IComparable<Node>
    {
        // The byte symbol for this node. Only used for leaf nodes.
        public byte Symbol { get; set; }

        // The frequency of the symbol or the combined frequency of its children.
        public int Frequency { get; set; }

        // The left child of this node in the tree.
        public Node Left { get; set; }

        // The right child of this node in the tree.
        public Node Right { get; set; }

        /// <summary>
        /// Compares this node to another node based on frequency.
        /// This is essential for the priority queue (or sorting) to build the tree correctly.
        /// Lower frequency nodes have higher priority.
        /// </summary>
        /// <param name="other">The other node to compare to.</param>
        /// <returns>An integer indicating the relative order.</returns>
        public int CompareTo(Node other)
        {
            if (other == null) return 1;

            return this.Frequency.CompareTo(other.Frequency);
        }
    }
}
