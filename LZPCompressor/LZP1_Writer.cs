using System.Collections.Generic;

namespace LZPCompressor
{
    public sealed partial class LZP1Compressor
    {
        /// <summary>
        /// Contain logic to encode values and write them into virtual output
        /// </summary>
        private sealed class LZP1ContentWriter
        {
            private readonly List<byte> _arr = new List<byte>(); // Output array
            private int _workingByte = 0; // Current modifying byte
            private int _bitsBusy = 0; // Count of bits we modifyed in _workingByte

            /// <summary>
            /// Write literal to output
            /// </summary>
            public void WriteLiteral(byte b)
            {
                // _bitsBusy will not changed
                // Write to workingbyte first bitsBusy bits of b and flush
                // Another part of b write to new workingByte
                _arr.Add((byte) (_workingByte + (b >> _bitsBusy)));
                _workingByte = (b << (8 - _bitsBusy)) & 0xFF;
            }

            /// <summary>
            /// Write bit to output
            /// </summary>
            public void WriteFlag(bool bit)
            {
                // Move bitsBusy
                // If true - write 1 to current bit
                // Else 0 will be written (virtually) by moving bitsBusy
                ++_bitsBusy;
                if (bit)
                    _workingByte += 1 << (8 - _bitsBusy);

                // If workingByte is full, flush it
                if (_bitsBusy == 8)
                {
                    _arr.Add((byte) _workingByte);
                    _workingByte = 0;
                    _bitsBusy = 0;
                }
            }

            /// <summary>
            /// Return current content of LZP1ContentWriter
            /// </summary>
            public byte[] GetArray()
            {
                // If there is some data in workingByte
                // Set last bits of it to 1
                if (_bitsBusy != 0)
                {
                    int b = (1 << (8 - _bitsBusy)) - 1;
                    _arr.Add((byte) (_workingByte + b));
                }
                return _arr.ToArray();
            }

            /// <summary>
            /// Encode length and write it
            /// </summary>
            public void WriteLength(int length)
            {
                // Write segment of 1 bit to detect length of 1
                if (length == 1)
                {
                    WriteFlag(false);
                    return;
                }
                WriteFlag(true);

                // Decrease length as it is not 1 and 
                // After encode 1 --> 00
                //              2 --> 01
                length -= 2;

                // Write length by segments until whole length is encoded
                int bits = 2;
                if (WriteLengthSegment(ref length, bits))
                    return;
                bits = 3;
                if (WriteLengthSegment(ref length, bits))
                    return;
                bits = 5;
                if (WriteLengthSegment(ref length, bits))
                    return;
                bits = 8;
                // Encoded length may contain many segments of 8 bits
                while (!WriteLengthSegment(ref length, bits))
                {
                }
            }

            /// <summary>
            /// Write segment of length and decrease length by written value
            /// </summary>
            /// <param name="length">Length to write</param>
            /// <param name="bits">Length of segment</param>
            /// <returns>True, if whole length is encoded</returns>
            private bool WriteLengthSegment(ref int length, int bits)
            {
                // If length can fit in segment
                if (length + 1 < 1 << bits)
                {
                    // Just encode length and return true
                    for (int k = 1 << (bits - 1); k > 0; k >>= 1)
                        WriteFlag((length & k) != 0);
                    return true;
                }
                // If length can not fit in segment
                // Write 1's and decrease length
                for (int i = 0; i < bits; i++)
                    WriteFlag(true);
                length -= (1 << bits) - 1;
                return false;
            }
        }
    }
}
