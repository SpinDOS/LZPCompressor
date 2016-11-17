using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LZPCompressor
{
    class LZP1
    {
        private ushort Hash(byte x, byte y, byte z) => (ushort)(((x << 8) + z) ^ (y << 4));

        public byte[] Compress(byte[] input)
        {
            OutputWriter output = new OutputWriter();
            int[] table = new int[ushort.MaxValue + 1];
            for (int i = 0; i < table.Length; i++)
                table[i] = -1;
            output.WriteByte(input[0]);
            output.WriteByte(input[1]);
            output.WriteByte(input[2]);
            int curPos = 3;
            while (curPos < input.Length - 1)
            {
                ushort hash = Hash(input[curPos - 3], input[curPos - 2], input[curPos - 1]);
                int foundPos = table[hash];
                table[hash] = curPos;
                if (foundPos >= 0 && input[curPos] == input[foundPos]) // match
                {
                    output.WriteFlag(false);
                    output.WriteFlag(false);
                }
                else
                {
                    byte literal = input[curPos++];
                    hash = Hash(input[curPos - 3], input[curPos - 2], input[curPos - 1]);
                    foundPos = table[hash];
                    table[hash] = curPos;
                    if (foundPos >= 0 && input[curPos] == input[foundPos]) // literal + match
                    {
                        output.WriteFlag(false);
                        output.WriteFlag(true);
                        output.WriteByte(literal);
                    }
                    else // 2 literals
                    {
                        output.WriteFlag(true);
                        output.WriteByte(literal);
                        output.WriteByte(input[curPos++]);
                        continue;
                    }
                }

                int length = FindLength(curPos, foundPos, input);

                if (length == 1)
                    output.WriteFlag(false);
                else
                {
                    output.WriteFlag(true);
                    output.WriteLength(length - 1);
                }
                curPos += length;
            }

            if (curPos == input.Length - 1)
            {
                output.WriteFlag(true);
                output.WriteByte(input[curPos]);
            }
            return output.GetArray();
        }

        private int FindLength(int cur, int fromTable, byte[] arr)
        {
            int length = 0;
            while (cur < arr.Length && arr[cur++] == arr[fromTable++])
                ++length;
            return length;
        }

        public byte[] Decompress(byte[] input)
        {
            List<byte> output = new List<byte>(input.Length);
            InputReader reader = new InputReader(input);
            int[] table = new int[ushort.MaxValue + 1];

            output.Add(reader.ReadByte());
            output.Add(reader.ReadByte());
            output.Add(reader.ReadByte());
            int curPos = 3;

            while (reader.CanReadFlag)
            {
                if (reader.ReadFlag()) // 2 literals
                {
                    if (!reader.CanRead2Bytes)
                        break;
                    output.Add(reader.ReadByte());
                    table[Hash(output[curPos - 3], output[curPos - 2], output[curPos - 1])] = curPos++;
                    output.Add(reader.ReadByte());
                    table[Hash(output[curPos - 3], output[curPos - 2], output[curPos - 1])] = curPos++;
                }
                else
                {
                    if (reader.ReadFlag()) // literal + match
                    {
                        output.Add(reader.ReadByte());
                        table[Hash(output[curPos - 3], output[curPos - 2], output[curPos - 1])] = curPos++;
                    }
                    // match
                    ushort hash = Hash(output[curPos - 3], output[curPos - 2], output[curPos - 1]);
                    int foundPos = table[hash];
                    table[hash] = curPos++;
                    if (!reader.ReadFlag())
                    {
                        output.Add(output[foundPos]);
                    }
                    else
                    {
                        int length = reader.ReadLength();
                        curPos += length - 1;
                        while (length-- > 0)
                            output.Add(output[foundPos++]);
                    }
                }
            }

            if (reader.CanReadByte)
                output.Add(reader.ReadByte());

            return output.ToArray();
        }

    }

    internal sealed class OutputWriter
    {
        private readonly List<byte> _arr = new List<byte>();
        private int _workingByte = 0;
        private int _bitsBusy = 0;

        public void WriteByte(int b)
        {
            _arr.Add((byte) (_workingByte + (b >> _bitsBusy)));
            _workingByte = (b << (8 - _bitsBusy)) & 0xFF;
        }

        public void WriteFlag(bool bit)
        {
            ++_bitsBusy;
            if (bit)
                _workingByte += 1 << (8 - _bitsBusy);
            if (_bitsBusy == 8)
            {
                _arr.Add((byte)_workingByte);
                _workingByte = 0;
                _bitsBusy = 0;
            }
        }

        public byte[] GetArray()
        {
            if (_bitsBusy != 0)
            {
                int b = (1 << (8 - _bitsBusy)) - 1;
                _arr.Add((byte) (_workingByte + b));
            }
            return _arr.ToArray();
        }

        public void WriteLength(int length)
        {
            --length;
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
            while (!WriteLengthSegment(ref length, bits))
            { }
        }

        private bool WriteLengthSegment(ref int length, int bits)
        {
            if (length + 1 < 1 << bits)
            {
                for (int i = 1 << (bits - 1); i > 0; i >>= 1)
                    WriteFlag((length & i) != 0);
                return true;
            }
            for (int i = 0; i < bits; i++)
                WriteFlag(true);
            length -= (1 << bits) - 1;
            return false;
        }
    }

    internal sealed class InputReader
    {
        private readonly byte[] _arr;
        private int _currentPos = 0;
        private byte _notReadBits = 8;
        private byte _workingByte;

        public bool CanReadByte => _currentPos < _arr.Length - 1;
        public bool CanRead2Bytes => _currentPos < _arr.Length - 2;
        public bool CanReadFlag => CanRead2Bytes || _notReadBits > 0;

        public InputReader(byte[] input)
        {
            _arr = input;
            _workingByte = _arr[0];
        }

        public bool ReadFlag()
        {
            if (_notReadBits == 0)
            {
                _workingByte = _arr[++_currentPos];
                _notReadBits = 8;
            }
            return (_workingByte & 1 << --_notReadBits) != 0;
        }

        public byte ReadByte()
        {
            byte result = (byte) (_workingByte << (8 - _notReadBits));
            _workingByte = _arr[++_currentPos];
            return (byte) (result + (_workingByte >> _notReadBits));
        }

        public int ReadLength()
        {
            int length = 1;
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
            while (!ReadLengthSegment(ref length, bits))
            { }
            return length;
        }

        private bool ReadLengthSegment(ref int length, int bitCount)
        {
            bool result = false;
            for (int i = 1 << (bitCount - 1); i > 0; i >>= 1)
            {
                if (ReadFlag())
                    length += i;
                else
                    result = true;
            }
            if (result)
                length += 1;
            return result;
        }
    }

}