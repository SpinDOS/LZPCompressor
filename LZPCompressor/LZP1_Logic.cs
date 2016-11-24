using System;
using System.Collections.Generic;

namespace LZPCompressor
{
    /// <summary>
    /// Contain logic for using LZP-1 compression algorithm
    /// </summary>
    internal partial class LZP1Compressor : ICompressor
    {
        // Hash 24 bit -> 16 bit
        private static ushort Hash(byte x, byte y, byte z) => (ushort) (((x << 8) + z) ^ (y << 4));

        public byte[] Compress(byte[] input)
        {
            // Check input
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (input.Length < 4)
                throw new BadInputException("Input length is too small");

            // Initialization
            LZP1ContentWriter output = new LZP1ContentWriter();
            int[] table = new int[ushort.MaxValue + 1];
            for (int i = 0; i < table.Length; i++)
                table[i] = -1;

            // Write first 3 bytes
            output.WriteLiteral(input[0]);
            output.WriteLiteral(input[1]);
            output.WriteLiteral(input[2]);
            int curPos = 3;

            // Loop through whole array until pre-last byte 
            // In the end it stops on the end (if match) or 
            // before last not-matched byte
            while (curPos < input.Length - 1)
            {
                // Working with current context (previous 3 literals)
                ushort hash = Hash(input[curPos - 3], input[curPos - 2], input[curPos - 1]);
                int foundPos = table[hash];
                table[hash] = curPos;

                // Handling current literal

                if (foundPos == -1 || input[curPos] != input[foundPos]) // not match
                {
                    // Saving current literal and handling next one
                    byte literal = input[curPos++];
                    hash = Hash(input[curPos - 3], input[curPos - 2], input[curPos - 1]);
                    foundPos = table[hash];
                    table[hash] = curPos;

                    if (foundPos == -1 || input[curPos] != input[foundPos]) // 2 literals
                    {
                        // Write flag, 2 literals without compressing
                        output.WriteFlag(true);
                        output.WriteLiteral(literal);
                        output.WriteLiteral(input[curPos++]);
                        continue; // goto next byte
                    }
                    else // Literal + match
                    {
                        // Write flags and not-matching literal
                        output.WriteFlag(false);
                        output.WriteFlag(true);
                        output.WriteLiteral(literal);
                        // goto write length
                    }
                }
                else // Match
                {
                    output.WriteFlag(false);
                    output.WriteFlag(false);
                    // goto write length
                }

                // Write matching length
                int length = FindLength(curPos, foundPos, input);
                output.WriteLength(length);
                curPos += length; // moving through input array
            }

            // If loop stopped before last byte
            // If not - all bytes were written
            if (curPos == input.Length - 1)
            {
                // Write with common format (with control bit) for valid reading
                output.WriteFlag(true);
                output.WriteLiteral(input[curPos]);
            }
            return output.GetArray();
        }

        // Return length of matching strings starting from cur and fromTable
        private static int FindLength(int cur, int fromTable, byte[] arr)
        {
            int length = 0;
            while (cur < arr.Length && arr[cur++] == arr[fromTable++])
                ++length;
            return length;
        }

        
        public byte[] Decompress(byte[] input)
        {
            // Initialization
            List<byte> output = new List<byte>(input.Length);
            LZP1ContentReader reader = new LZP1ContentReader(input);
            int[] table = new int[ushort.MaxValue + 1];

            // Read first 3 bytes
            output.Add(reader.ReadByte());
            output.Add(reader.ReadByte());
            output.Add(reader.ReadByte());
            int curPos = 3; // It's always equal to output.Count

            try
            {
                // Loop through input while can read flag
                // As we added to last byte bits of 1 while compressing
                // last byte of input can contain 0 flag and then length
                // or 1 flag and next n bits of 1 - so we check it in the inner if
                while (reader.CanReadFlag)
                {
                    // Read first control bit
                    if (reader.ReadFlag()) // 2 literals
                    {
                        // If literal + endOfFile or simply endOfFile
                        if (!reader.CanRead2Bytes)
                            break;

                        // Read 2 literals and add them to the hashtable
                        output.Add(reader.ReadByte());
                        table[Hash(output[curPos - 3], output[curPos - 2], output[curPos - 1])] = curPos++;
                        output.Add(reader.ReadByte());
                        table[Hash(output[curPos - 3], output[curPos - 2], output[curPos - 1])] = curPos++;
                    }
                    else
                    {
                        // Read second control flag
                        if (reader.ReadFlag()) // literal + match
                        {
                            // Read literal and handle it
                            output.Add(reader.ReadByte());
                            table[Hash(output[curPos - 3], output[curPos - 2], output[curPos - 1])] = curPos++;
                            // goto handle next byte and read length
                        }
                        // match

                        // Add current byte to hashtable
                        // Literal of this byte will be written in next step
                        ushort hash = Hash(output[curPos - 3], output[curPos - 2], output[curPos - 1]);
                        int foundPos = table[hash];
                        table[hash] = curPos;

                        // Read length of matching string and copy it from foundPos
                        int length = reader.ReadLength();
                        curPos += length;
                        while (length-- > 0)
                            output.Add(output[foundPos++]);
                    }
                }

                // If loop ended with 2 literals flag but input did not have 2 bytes
                if (reader.CanReadByte)
                    output.Add(reader.ReadByte());
            }
            catch (IndexOutOfRangeException)
            {
                throw new NotLZP1InputException();
            }

            return output.ToArray();
        }

    }

}