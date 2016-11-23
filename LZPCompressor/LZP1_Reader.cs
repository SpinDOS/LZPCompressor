using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LZPCompressor
{
    internal partial class LZP1Compressor
    {
        /// <summary>
        /// Contain logic to decode values from input array with compressed data
        /// </summary>
        internal sealed class LZP1ContentReader
        {
            private readonly byte[] _arr; // Input array
            // Position for current working byte in input array
            private int _currentPos = 0; 
            // Count of not read bits in workingByte
            private byte _notReadBits = 8;
            private byte _workingByte; // Byte we are working with right now

            public bool CanReadByte => _currentPos < _arr.Length - 1;
            public bool CanRead2Bytes => _currentPos < _arr.Length - 2;
            public bool CanReadFlag => CanReadByte || _notReadBits > 0;

            public LZP1ContentReader(byte[] input)
            {
                // Initialize input array and workingByte
                _arr = input;
                _workingByte = _arr[0];
            }

            /// <summary>
            /// Read bit from input
            /// </summary>
            /// <returns>True if current bit is 1</returns>
            public bool ReadFlag()
            {
                // If there is no not read bits in working byte then read next one
                if (_notReadBits == 0)
                {
                    _workingByte = _arr[++_currentPos];
                    _notReadBits = 8;
                }
                return (_workingByte & 1 << --_notReadBits) != 0;
            }

            /// <summary>
            /// Read byte from input
            /// </summary>
            public byte ReadByte()
            {
                // Add to result not read bits from workingByte
                // Read next byte and add to result leftover bits
                byte result = (byte) (_workingByte << (8 - _notReadBits));
                _workingByte = _arr[++_currentPos];
                return (byte) (result + (_workingByte >> _notReadBits));
            }

            /// <summary>
            /// Read length from input
            /// </summary>
            public int ReadLength()
            {
                // Read first flag
                if (!ReadFlag())
                    return 1;

                int length = 1;

                // Read segments until 
                // some segment says that length has fit there
                int bits = 2;
                if (ReadLengthSegment(ref length, bits))
                    return length;
                bits = 3;
                if (ReadLengthSegment(ref length, bits))
                    return length;
                bits = 5;
                if (ReadLengthSegment(ref length, bits))
                    return length;
                bits = 8;
                // Encoded length may contain many segments of 8 bits
                while (!ReadLengthSegment(ref length, bits))
                { }
                return length;
            }

            /// <summary>
            /// Read segment from input
            /// Increase length by encoded value
            /// </summary>
            /// <param name="length">Value of length to change</param>
            /// <param name="bitCount">Length of segment</param>
            /// <returns>True is length has fit in the segment</returns>
            private bool ReadLengthSegment(ref int length, int bitCount)
            {
                bool result = false; // By default return false
                for (int k = 1 << (bitCount - 1); k > 0; k >>= 1)
                {
                    if (ReadFlag())
                        length += k;
                    else // If there is at least one 0 then return true
                        result = true;
                }
                // If length has fit then it's value is greater that read value
                if (result)
                    length += 1;
                return result;
            }

        }
    }
}
