using System;
using System.Collections.Generic;
using System.Linq;

namespace JeopardyNesTextTool.Model
{
    public interface ITreeNode
    {
    }

    public class LeafNode : ITreeNode
    {

        public char Value { get; set; }
        public LeafNode(char value) => Value = value;
    }

    public class InternalNode : ITreeNode
    {
        public ITreeNode Left;
        public ITreeNode Right;
        public Dictionary<char, bool[]> PathDictionary = new();

        public void Deserialize(byte[] treeBytes, byte offset)
        {
            var leftByte = treeBytes[offset];
            if (leftByte < 0x80)
            {
                Left = new LeafNode(Convert.ToChar(leftByte));
            }
            else
            {
                var nextOffset = (byte)(leftByte << 1);
                var leftNode = new InternalNode();
                leftNode.Deserialize(treeBytes, nextOffset);
                Left = leftNode;
            }
            var rightByte = treeBytes[offset + 1];
            if (rightByte < 0x80)
            {
                Right = new LeafNode(Convert.ToChar(rightByte));
            }
            else
            {
                var nextOffset = (byte)(rightByte << 1);
                var rightNode = new InternalNode();
                rightNode.Deserialize(treeBytes, nextOffset);
                Right = rightNode;
            }
        }
        /// <summary>
        /// Default constructor for empty node
        /// </summary>
        public InternalNode()
        {

        }
        /// <summary>
        /// Constructor for building the tree based on input string
        /// </summary>
        public InternalNode(string inputString)
        {
            //Dictionary with char-frequency pairs
            var histogramDictionary = inputString
                .GroupBy(c => c)
                .ToDictionary(g => g.Key, g => g.Count());

            if (histogramDictionary.Count < 2)
            {
                throw new InvalidOperationException("Need two or more chars for tree building");
            }

            //Sorted dictionary with tree nodes - frequency pairs, used for tree internal nodes construction

            var tupleComparer = Comparer<(ITreeNode, int)>.Create((a, b) =>
            {
                //First compare by frequency, if frequencies are equal, just compare objects - they are equal and will still be added to set
                var result = a.Item2.CompareTo(b.Item2);
                return result == 0 ? a.Item1.GetHashCode().CompareTo(b.Item1.GetHashCode()) : result;
            });
            var sortedSet = new SortedSet<(ITreeNode, int)>(tupleComparer);

            foreach (var histogramPair in histogramDictionary)
            {
                sortedSet.Add((new LeafNode(histogramPair.Key), histogramPair.Value));
            }

            while (sortedSet.Count > 1)
            {
                var left = sortedSet.Min;
                sortedSet.Remove(left);
                var right = sortedSet.Min;
                sortedSet.Remove(right);
                var internalNode = new InternalNode()
                {
                    Left = left.Item1,
                    Right = right.Item1
                };
                sortedSet.Add((internalNode, left.Item2 + right.Item2));
            }
            if (sortedSet.Min.Item1 is not InternalNode root)
            {
                throw new InvalidOperationException("Leaf node is found as root during tree building");
            }
            Left = root.Left;
            Right = root.Right;
        }




        public char DecodeChar(IEnumerator<bool> bitsEnumerator)
        {
            ITreeNode currentNode = this;
            while (currentNode is InternalNode internalNode)
            {
                bitsEnumerator.MoveNext();
                var bitIsSet = bitsEnumerator.Current;
                currentNode = bitIsSet ? internalNode.Right : internalNode.Left;
            }

            if (currentNode is LeafNode leafNode)
            {
                return leafNode.Value;
            }

            throw new NotImplementedException("Unknown node type");
        }

        /// <summary>
        /// Sets up all char paths for further char encoding by tree
        /// </summary>
        public void SetTreeCharsPaths()
        {
            SetTreeCharsPathsCore(this, Enumerable.Empty<bool>());
        }
        private void SetTreeCharsPathsCore(ITreeNode node, IEnumerable<bool> path)
        {

            switch (node)
            {
                case LeafNode leafNode:
                    PathDictionary.Add(leafNode.Value, path.ToArray());
                    break;
                case InternalNode internalNode:
                    SetTreeCharsPathsCore(internalNode.Left, path.Append(false));
                    SetTreeCharsPathsCore(internalNode.Right, path.Append(true));
                    break;
                default: throw new NotImplementedException("Unknown node type");
            }
        }

        public bool[] EncodeString(string str)
        {
            return str.SelectMany(EncodeChar).ToArray();
        }

        private bool[] EncodeChar(char encodeChar)
        {
            return PathDictionary[encodeChar];
        }

        /// <summary>
        /// Serialize tree in game's store format, must fit in given target size
        /// </summary>
        /// <returns>bytes block of given size</returns>
        /*
         *The game built tree with bottom up approach, resulting in lowest chars were serialized first
         * and forming the resulting tree size. Decoding in assembly uses heavily this size byte in several places,
         * and decoding functions are also found in several places in ROM.
         * To avoid assembly code modification in multiple places it's best not to alter original tree size.
         * The approach is to serialize in original format in reverse order and then prepend this block with empty bytes
         */
        public byte[] Serialize(int targetSize)
        {
            var nodesPlainList = new List<ITreeNode>();
            var queue = new Queue<ITreeNode>();
            queue.Enqueue(Right);
            queue.Enqueue(Left);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                nodesPlainList.Insert(0, node);
                if (node is not InternalNode internalNode)
                {
                    continue;
                }
                queue.Enqueue(internalNode.Right);
                queue.Enqueue(internalNode.Left);
            }
            var paddingSize = targetSize - nodesPlainList.Count;
            if (paddingSize < 0)
            {
                throw new InvalidOperationException(
                    $"Tree size is {nodesPlainList.Count}, cannot fit in target size {targetSize}");
            }

            var paddingNodeBlock = Enumerable.Repeat<ITreeNode>(null, paddingSize);
            nodesPlainList.InsertRange(0, paddingNodeBlock);


            var serializedBytes = new byte[targetSize];
            for (var i = 0; i < nodesPlainList.Count; i++)
            {
                switch (nodesPlainList[i])
                {
                    case null:
                        serializedBytes[i] = 0xFF;
                        break;
                    case LeafNode leafNode:
                        serializedBytes[i] = (byte)leafNode.Value;
                        break;
                    case InternalNode internalNode:
                        var leftOffset = nodesPlainList.IndexOf(internalNode.Left);
                        if (leftOffset == -1)
                        {
                            throw new InvalidOperationException("Left offset of internal node not found in nodes list");
                        }
                        if (leftOffset % 2 != 0)
                        {
                            throw new InvalidOperationException("Reference offset on left tree node is odd");
                        }
                        var serializedLeftOffset = leftOffset >> 1;//left offset in tree is always even and low bit is not used
                        if (serializedLeftOffset > 0x7F)
                        {
                            throw new InvalidOperationException(
                                $"Node offset in serialized table is {serializedLeftOffset}. Can't serialize in byte");
                        }
                        serializedBytes[i] = (byte)(serializedLeftOffset | 0x80);
                        break;
                    default: throw new NotImplementedException("Unknown node type");
                }
            }
            return serializedBytes;
        }


        /// <summary>
        /// Get tree chars in order of code ascending (for debug purposes)
        /// </summary>
        public List<char> GetChars()
        {
            static void CheckNode(ICollection<char> chars, Queue<InternalNode> internalNodes, ITreeNode node)
            {
                switch (node)
                {
                    case LeafNode leafNode:
                        chars.Add(leafNode.Value);
                        break;
                    case InternalNode internalNode:
                        internalNodes.Enqueue(internalNode);
                        break;
                    default: throw new NotImplementedException("Node type is not implemented");
                }
            }

            var result = new List<char>();
            var queue = new Queue<InternalNode>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                CheckNode(result, queue, node.Left);
                CheckNode(result, queue, node.Right);
            }

            return result;
        }
    }
}
