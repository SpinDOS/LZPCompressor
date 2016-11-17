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
            int cur = 3;
            while (cur < input.Length - 1)
            {
                ushort hash = Hash(input[cur - 3], input[cur - 2], input[cur - 1]);
                int pos = table[hash];
                table[hash] = cur;
                if (pos >= 0 && input[cur] == input[pos]) // match
                {
                    output.WriteFlag(false);
                    output.WriteFlag(false);
                }
                else
                {
                    byte literal = input[cur++];
                    hash = Hash(input[cur - 3], input[cur - 2], input[cur - 1]);
                    pos = table[hash];
                    table[hash] = cur;
                    if (pos >= 0 && input[cur] == input[pos]) // literal + match
                    {
                        output.WriteFlag(false);
                        output.WriteFlag(true);
                        output.WriteByte(literal);
                    }
                    else // 2 literals
                    {
                        output.WriteFlag(true);
                        output.WriteByte(literal);
                        output.WriteByte(input[cur++]);
                        continue;
                    }
                }
                int length = FindLength(cur, pos, input);
                WriteLengthToOutput(length, output);
                cur += length;
            }

            if (cur == input.Length - 1)
            {
                output.WriteFlag(true);
                output.WriteByte(input[cur]);
            }
            return output.GetArray();
        }

        private int FindLength(int cur, int fromTable, byte[] arr)
        {
            int result = 0;
            while (cur < arr.Length && arr[cur++] == arr[fromTable++])
                result++;
            return result;
        }

        private void WriteLengthToOutput(int length, OutputWriter output)
        {
            if (length == 1)
                output.WriteFlag(false);
            else
            {
                output.WriteFlag(true);
                output.WriteLength(length - 1);
            }
        }

        public byte[] Decompress(byte[] input)
        {
            return new byte[0];
            List<byte> output = new List<byte>(input.Length);
            InputReader reader = new InputReader(input.Skip(4));
            int[] table = new int[ushort.MaxValue + 1];

            int curOut = 3;

            while (true)
            {
                if (reader.ReadFlag()) // 2 literals
                {
                    output.Add(reader.ReadByte());
                    table[Hash(output[curOut - 3], output[curOut - 2], output[curOut - 1])] = curOut++;
                    output.Add(reader.ReadByte());
                    table[Hash(output[curOut - 3], output[curOut - 2], output[curOut - 1])] = curOut++;
                }
                else
                {
                    if (reader.ReadFlag()) // literal + match
                    {
                        output.Add(reader.ReadByte());
                        table[Hash(output[curOut - 3], output[curOut - 2], output[curOut - 1])] = curOut++;
                    }
                    // match
                    ushort hash = Hash(output[curOut - 3], output[curOut - 2], output[curOut - 1]);
                    int pos = table[hash];
                    table[hash] = curOut++;
                    if (!reader.ReadFlag())
                    {
                        output.Add(output[pos]);
                    }
                    else
                    {
                        int length = reader.ReadLength();
                        curOut += length - 1;
                        while (length-- > 0)
                            output.Add(output[pos++]);
                    }
                    
                }
            }

            //if (cur == inputLength - 1)
            //{
            //    byte b = reader.ReadByte();
            //    if (b != 0)
            //        output.Add(b);
            //}
            return output.ToArray();
        }

    }

    class OutputWriter
    {
        private readonly List<byte> result = new List<byte>();
        private int WorkingByte = 0;
        private int BitsBusy = 0;

        public void WriteByte(int b)
        {
            result.Add((byte) (WorkingByte + (b >> BitsBusy)));
            WorkingByte = (b << (8 - BitsBusy)) & 0xFF;
        }

        public void WriteFlag(bool bit)
        {
            ++BitsBusy;
            if (bit)
                WorkingByte += 1 << (8 - BitsBusy);
            if (BitsBusy == 8)
            {
                result.Add((byte)WorkingByte);
                WorkingByte = 0;
                BitsBusy = 0;
            }
        }

        public byte[] GetArray()
        {
            if (BitsBusy != 0)
                result.Add((byte)WorkingByte);
            return result.ToArray();
        }

        //public void WriteLength(int length)
//        {
//            --length;
//            if (length < 0)
//                throw new ArgumentException(nameof(length));
//            int bits = 2;
//            if (WriteLengthSegment(ref length, bits))
//                return;
//            bits = 3;
//            if (WriteLengthSegment(ref length, bits))
//                return;
//            bits = 5;
//            if (WriteLengthSegment(ref length, bits))
//                return;
//            bits = 8;
//            while (!WriteLengthSegment(ref length, bits))
//            { }
//        }

        //        private bool WriteLengthSegment(ref int length, int bits)
        //        {
        //            if (length + 1 < 1 << bits)
        //            {
        //                for (int i = bits - 1; i >= 0; i--)
        //                    WriteFlag((length & 1 << i) != 0);
        //                return true;
        //            }
        //            else
        //            {
        //                for (int i = 0; i < bits; i++)
        //                    WriteFlag(true);
        //                length -= (1 << bits) - 1;
        //                return false;
        //            }
        //        }

        //public void WriteLength(int length)
        //{
        //    --length;
        //    if (length < 3) // 2 bits to write
        //    {
        //        if (BitsBusy < 6)
        //        {
        //            WorkingByte += length << (6 - BitsBusy);
        //            BitsBusy += 2;
        //        }
        //        else if (BitsBusy == 6)
        //        {
        //            result.Add((byte) (WorkingByte + length));
        //            WorkingByte = 0;
        //            BitsBusy = 0;
        //        }
        //        else
        //        {
        //            if (length == 2)
        //                result.Add((byte) (WorkingByte + 1));
        //            else
        //                result.Add((byte) WorkingByte);
        //            WorkingByte = length % 2 == 1 ? 128 : 0;
        //            BitsBusy = 1;
        //        }
        //        return;
        //    }

        //    if (length < 10) // 5 bits to write
        //    {
        //        int toWrite = 21 + length; // 24(binary 11xxx) + length - 3
        //        if (BitsBusy < 3)
        //        {
        //            WorkingByte += toWrite << (3 - BitsBusy);
        //            BitsBusy += 5;
        //        }
        //        else if (BitsBusy == 3)
        //        {
        //            result.Add((byte) (WorkingByte + toWrite));
        //            WorkingByte = 0;
        //            BitsBusy = 0;
        //        }
        //        else
        //        {
        //            result.Add((byte) (WorkingByte +
        //                (toWrite >> (BitsBusy - 3))));
        //            BitsBusy = BitsBusy - 3;
        //            WorkingByte = toWrite << (8 - BitsBusy);
        //        }
        //        return;
        //    }

        //    //write first 2 bits
        //    if (BitsBusy < 6)
        //    {
        //        WorkingByte += 3 << (6 - BitsBusy);
        //        BitsBusy += 2;
        //    }
        //    else if (BitsBusy == 6)
        //    {
        //        result.Add((byte) (WorkingByte + 3));
        //        WorkingByte = 0;
        //        BitsBusy = 0;
        //    }
        //    else
        //    {
        //        result.Add((byte) (WorkingByte + 1));
        //        WorkingByte = 128;
        //        BitsBusy = 1;
        //    }

        //    if (length < 41) // from 10 to 40
        //    {
        //        WriteByte(214 + length); // 224(binary 111xxxxx) + length - 10
        //        return;
        //    }

        //    length -= 41;
        //    WriteByte(0xFF);
        //    while (length >= 0xFF)
        //    {
        //        WriteByte(0xFF);
        //        length -= 255;
        //    }

        //    WriteByte(length);
        //}
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
                for (int i = bits - 1; i >= 0; i--)
                    WriteFlag((length & 1 << i) != 0);
                return true;
            }
            else
            {
                for (int i = 0; i < bits; i++)
                    WriteFlag(true);
                length -= (1 << bits) - 1;
                return false;
            }
        }
    }

    class InputReader
    {
        private IEnumerator<byte> Input;

        public byte NotReadBits { get; private set; } = 8;

        public byte WorkingByte { get; private set; }
        public InputReader(IEnumerable<byte> input)
        {
            Input = input.GetEnumerator();
            WorkingByte = Input.Current;
        }

        public bool ReadFlag()
        {
            bool result = (WorkingByte & 1 << --NotReadBits) != 0;
            if (NotReadBits == 0)
            {
                Input.MoveNext();
                WorkingByte = Input.Current;
                NotReadBits = 8;
            }
            return result;
        }

        public byte ReadByte()
        {
            byte result = (byte)(WorkingByte << (8 - NotReadBits));
            if (Input.MoveNext())
            {
                WorkingByte = Input.Current;
                result = (byte) (result + (WorkingByte >> NotReadBits));
            }
            return result;
        }

        public int ReadLength()
        {
            int length = 0;
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
            return length + 1;
        }

        private bool ReadLengthSegment(ref int length, int bitCount)
        {
            bool result = false;
            for (int i = bitCount - 1; i >= 0; i--)
            {
                if (ReadFlag())
                    length += 1 << i;
                else
                    result = true;
            }
            if (result)
                length += 1;
            return result;
        }
    }

}