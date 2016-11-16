using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LZPCompressor
{
    class LZP1
    {
        public byte[] Compress(byte[] input)
        {
            OutputWriter output = new OutputWriter();
            Dictionary<short, int> table = new Dictionary<short, int>(1 << 12);
            output.WriteByte(input[0]);
            output.WriteByte(input[1]);
            output.WriteByte(input[2]);
            int cur = 3;
            int inputLength = input.Length;
            while (cur < inputLength - 1)
            {
                short hash = Hash(input[cur - 3], input[cur - 2], input[cur - 1]);
                int pos;
                bool found = table.TryGetValue(hash, out pos);
                table[hash] = cur;
                if (found && input[cur] == input[pos]) // match
                {
                    output.WriteFlag(false);
                    output.WriteFlag(false);
                    int length = FindLength(cur, pos, input);
                    output.WriteLength(length);
                    cur += length;
                }
                else
                {
                    byte literal = input[cur++];
                    hash = Hash(input[cur - 3], input[cur - 2], input[cur - 1]);
                    found = table.TryGetValue(hash, out pos);
                    table[hash] = cur;
                    if (found && input[cur] == input[pos]) // literal + match
                    {
                        output.WriteFlag(false);
                        output.WriteFlag(true);
                        output.WriteByte(literal);
                        int length = FindLength(cur, pos, input);
                        output.WriteLength(length);
                        cur += length;
                    }
                    else // 2 literals
                    {
                        output.WriteFlag(true);
                        output.WriteByte(literal);
                        output.WriteByte(input[cur++]);
                    }
                }
            }

            if (cur == inputLength - 1)
            {
                output.WriteFlag(true);
                output.WriteByte(input[cur]);
            }
            return output.GetArray();
        }

        public byte[] Decompress(byte[] input)
        {
            List<byte> output = new List<byte>(input.Length);
            InputReader reader = new InputReader(input);
            Dictionary<short, int> table = new Dictionary<short, int>(1 << 12);
            int inputLength = input.Length;
            output.Add(reader.ReadByte());
            output.Add(reader.ReadByte());
            output.Add(reader.ReadByte());

            while (reader.CurrentPosition < inputLength - 1)
            {
                if (reader.ReadFlag()) // 2 literals
                {
                    int curpos = output.Count;
                    output.Add(reader.ReadByte());
                    if (reader.CurrentPosition >= inputLength - 1)
                        break;
                    table[Hash(output[curpos - 3], output[curpos - 2], output[curpos - 1])] = curpos++;
                    output.Add(reader.ReadByte());
                    table[Hash(output[curpos - 3], output[curpos - 2], output[curpos - 1])] = curpos;
                }
                else
                {
                    int curpos = output.Count;
                    if (reader.ReadFlag()) // literal + match
                    {
                        output.Add(reader.ReadByte());
                        table[Hash(output[curpos - 3], output[curpos - 2], output[curpos - 1])] = curpos++;
                    }
                    short hash = Hash(output[curpos - 3], output[curpos - 2], output[curpos - 1]);
                    int pos = table[hash];
                    table[hash] = curpos;
                    int length = reader.ReadLength();
                    for (int i = 0; i < length; i++)
                        output.Add(output[pos++]);
                }
            }

            if (reader.CurrentPosition == inputLength - 1)
            {
                byte b = reader.ReadByte();
                if (b != 0)
                    output.Add(b);
            }
            return output.ToArray();
        }

        private short Hash(byte x, byte y, byte z) => (short)(x ^ (y << 7) ^ (z << 11));

        private int FindLength(int cur, int fromTable, byte[] arr)
        {
            int result = 0;
            while (cur < arr.Length && arr[cur++] == arr[fromTable++])
                result++;
            return result;
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
        private byte[] input;
        public uint CurrentPosition { get; private set; } = 0;
        public byte NotReadBits { get; private set; } = 8;
        public InputReader(byte[] Input)
        {
            input = Input;
        }

        public bool ReadFlag()
        {
            if (CurrentPosition == input.Length)
                return false;
            bool result = (input[CurrentPosition] & 1 << --NotReadBits) != 0;
            if (NotReadBits == 0)
            {
                CurrentPosition++;
                NotReadBits = 8;
            }
            return result;
        }

        public byte ReadByte()
        {
            byte result = 0;
            for (int i = 7; i >= 0; i--)
            {
                if (ReadFlag())
                    result = (byte) (result + (1 << i));
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
            return length;
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




//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace LZPCompressor
//{
//    class LZP1
//    {
//        public byte[] Compress(byte[] input)
//        {
//            OutputWriter output = new OutputWriter();
//            Dictionary<Hash12Bit, int> table = new Dictionary<Hash12Bit, int>(1 << 12);
//            output.WriteByte(input[0]);
//            output.WriteByte(input[1]);
//            output.WriteByte(input[2]);
//            int cur = 3;
//            int inputLength = input.Length;
//            while (cur < inputLength - 1)
//            {
//                Hash12Bit hash = Hash(input[cur - 3], input[cur - 2], input[cur - 1]);
//                int pos;
//                bool found = table.TryGetValue(hash, out pos);
//                table[hash] = cur;
//                if (found && input[cur] == input[pos]) // match
//                {
//                    output.WriteFlag(false);
//                    output.WriteFlag(false);
//                    int length = FindLength(cur, pos, input);
//                    output.WriteLength(length);
//                    cur += length;
//                }
//                else
//                {
//                    byte literal = input[cur++];
//                    hash = Hash(input[cur - 3], input[cur - 2], input[cur - 1]);
//                    found = table.TryGetValue(hash, out pos);
//                    table[hash] = cur;
//                    if (found && input[cur] == input[pos]) // literal + match
//                    {
//                        output.WriteFlag(false);
//                        output.WriteFlag(true);
//                        output.WriteByte(literal);
//                        int length = FindLength(cur, pos, input);
//                        output.WriteLength(length);
//                        cur += length;
//                    }
//                    else // 2 literals
//                    {
//                        output.WriteFlag(true);
//                        output.WriteByte(literal);
//                        output.WriteByte(input[cur++]);
//                    }

//                }

//            }

//            if (cur == inputLength - 1)
//            {
//                output.WriteFlag(true);
//                output.WriteByte(input[cur]);
//            }
//            return output.GetArray();
//        }

//        public byte[] Decompress(byte[] input)
//        {
//            List<byte> output = new List<byte>(input.Length);
//            InputReader reader = new InputReader(input);
//            Dictionary<Hash12Bit, int> table = new Dictionary<Hash12Bit, int>(1 << 12);
//            int inputLength = input.Length;
//            output.Add(reader.ReadByte());
//            output.Add(reader.ReadByte());
//            output.Add(reader.ReadByte());

//            while (reader.CurrentPosition < inputLength - 1)
//            {
//                if (reader.ReadFlag()) // 2 literals
//                {
//                    int curpos = output.Count;
//                    output.Add(reader.ReadByte());
//                    if (reader.CurrentPosition >= inputLength - 1)
//                        break;
//                    table[Hash(output[curpos - 3], output[curpos - 2], output[curpos - 1])] = curpos++;
//                    output.Add(reader.ReadByte());
//                    table[Hash(output[curpos - 3], output[curpos - 2], output[curpos - 1])] = curpos;
//                }
//                else
//                {
//                    int curpos = output.Count;
//                    if (reader.ReadFlag()) // literal + match
//                    {
//                        output.Add(reader.ReadByte());
//                        table[Hash(output[curpos - 3], output[curpos - 2], output[curpos - 1])] = curpos++;
//                    }
//                    Hash12Bit hash = Hash(output[curpos - 3], output[curpos - 2], output[curpos - 1]);
//                    int pos = table[hash];
//                    table[hash] = curpos;
//                    int length = reader.ReadLength();
//                    for (int i = 0; i < length; i++)
//                        output.Add(output[pos++]);
//                }
//            }

//            if (reader.CurrentPosition == inputLength - 1)
//            {
//                byte b = reader.ReadByte();
//                if (b != 0)
//                    output.Add(b);
//            }
//            return output.ToArray();
//        }

//        public Hash12Bit Hash(byte x, byte y, byte z)
//        {
//            uint c = (uint) ((x << 16) + (y << 8) + z);
//            UInt16 h = (UInt16) (((c >> 11) ^ c) & 0xFFF);
//            return new Hash12Bit(h);
//        }

//        private int FindLength(int cur, int fromTable, byte[] arr)
//        {
//            int result = 0;
//            while (cur < arr.Length && arr[cur++] == arr[fromTable++])
//                result++;
//            if (result == 0)
//                throw new AggregateException();
//            return result;
//        }

//    }

//    struct Hash12Bit
//    {
//        public Hash12Bit(UInt16 value) { _value = value; }

//        private UInt16 _value;

//        public override int GetHashCode() => _value;

//        public override bool Equals(object obj)
//        {
//            if (!(obj is Hash12Bit))
//                return false;
//            Hash12Bit h = (Hash12Bit) obj;
//            return this._value == h._value;
//        }
//    }

//    class OutputWriter
//    {
//        MemoryStream output = new MemoryStream();
//        private byte current = 0;
//        private byte emptySpaceInCurrent = 8;

//        public void WriteByte(byte b)
//        {
//            for (int i = 7; i >= 0; i--)
//                WriteFlag((b & 1 << i) != 0);
//        }

//        public void WriteFlag(bool bit)
//        {
//            --emptySpaceInCurrent;
//            if (bit)
//                current += (byte) (1 << emptySpaceInCurrent);
//            if (emptySpaceInCurrent == 0)
//            {
//                output.WriteByte(current);
//                current = 0;
//                emptySpaceInCurrent = 8;
//            }
//        }

//        public byte[] GetArray()
//        {
//            if (emptySpaceInCurrent != 8)
//                output.WriteByte(current);
//            return output.ToArray();
//        }


//        public void WriteLength(int length)
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
//    }

//    class InputReader
//    {
//        private byte[] input;
//        public uint CurrentPosition { get; private set; } = 0;
//        public byte NotReadBits { get; private set; } = 8;
//        public InputReader(byte[] Input)
//        {
//            input = Input;
//        }

//        public bool ReadFlag()
//        {
//            if (CurrentPosition == input.Length)
//                return false;
//            bool result = (input[CurrentPosition] & 1 << --NotReadBits) != 0;
//            if (NotReadBits == 0)
//            {
//                CurrentPosition++;
//                NotReadBits = 8;
//            }
//            return result;
//        }

//        public byte ReadByte()
//        {
//            byte result = 0;
//            for (int i = 7; i >= 0; i--)
//            {
//                if (ReadFlag())
//                    result = (byte) (result + (1 << i));
//            }
//            return result;
//        }

//        public int ReadLength()
//        {
//            int length = 0;
//            int bits = 2;
//            if (ReadLengthSegment(ref length, bits))
//                return length;
//            bits = 3;
//            if (ReadLengthSegment(ref length, bits))
//                return length;
//            bits = 5;
//            if (ReadLengthSegment(ref length, bits))
//                return length;
//            bits = 8;
//            while (!ReadLengthSegment(ref length, bits))
//            { }
//            return length;
//        }

//        private bool ReadLengthSegment(ref int length, int bitCount)
//        {
//            bool result = false;
//            for (int i = bitCount - 1; i >= 0; i--)
//            {
//                if (ReadFlag())
//                    length += 1 << i;
//                else
//                    result = true;
//            }
//            if (result)
//                length += 1;
//            return result;
//        }
//    }

//}

