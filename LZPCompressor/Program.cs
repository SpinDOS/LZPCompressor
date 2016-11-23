using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;


// Реализовать компрессор для сжатия произвольных файлов на основе 
// одного из наиболее элегантных словарных методов – LZP. 
// Сравнить результативность по степени и по времени сжатия 
// с другими компрессорами, использующими LZP-подобные методы.


namespace LZPCompressor
{
    static class Program
    {
        static void Main(string[] args)
        {
            #region Check user input
            string usageInfo = @"Usage: LZPCompessor.exe <c\d> <input> <output>";

            if (args.Length != 3 ||
                (args[0].ToUpper() != "C" && args[0].ToUpper() != "D"))
            {
                Console.WriteLine(usageInfo);
                return;
            }

            bool isCompress = args[0].ToUpper() == "C";

            string inputFilename = args[1];
            if (!File.Exists(inputFilename))
            {
                Console.WriteLine($@"File {inputFilename} does not exist!");
                return;
            }
            byte[] inputArr;
            try
            {
                inputArr = File.ReadAllBytes(inputFilename);
            }
            catch (IOException)
            {
                Console.WriteLine($@"Some errors occured while reading {inputFilename}!");
                return;
            }
            catch (Exception e)
            {
                if (!(e is UnauthorizedAccessException) && !(e is SecurityException))
                    throw;
                Console.WriteLine($@"I do not have permissions to read {inputFilename}!");
                return;
            }

            if (inputArr.Length < 4)
            {
                Console.WriteLine($@"File {inputFilename} is too small! " +
                    @"Your must use files more than 4 bytes size");
                return;
            }

            string outputFilename = args[2];
            if (File.Exists(outputFilename))
            {
                string ans;
                do
                {
                    Console.Write($@"File {outputFilename} already exists. " +
                        @"Do you want to rewrite it? (y\n): ");
                    ans = Console.ReadLine()?.ToUpper();
                } while (ans != "Y" && ans != "N");
                if (ans == "Y")
                    File.Delete(outputFilename);
                else
                {
                    Console.WriteLine(usageInfo);
                    return;
                }
            } 
            #endregion

            FileStream output = null;
            ICompressor compressor = new LZP1Compressor(); 
            try
            {
                output = File.OpenWrite(outputFilename);
                var sw = Stopwatch.StartNew();
                byte[] result = isCompress ? compressor.Compress(inputArr) : compressor.Decompress(inputArr);
                sw.Stop();
                Console.WriteLine((isCompress ? "Compress" : "Decompress") +
                                  $@" complete in {sw.ElapsedMilliseconds} milliseconds");
                if (isCompress)
                {
                    Console.WriteLine(@"Compress ratio = {0:0.###} ({1} ---> {2})",
                        1.0*result.Length/inputArr.Length,
                        inputArr.Length, result.Length);
                }
                output.Write(result, 0, result.Length);
            }

            #region Handle errors
            catch (NotLZP1InputException)
            {
                Console.WriteLine($@"{inputFilename} is not LZP compressed file");
                return;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($@"I do not have permissions to write to {outputFilename}!");
                return;
            }
            catch (IOException)
            {
                Console.WriteLine($@"Some errors occured while creating or writing {outputFilename}!");
                return;
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine($@"File {inputFilename} is too big");
                return;
            }
            catch (Exception e)
            {
                if (e is NotSupportedException || e is ArgumentException)
                {
                    Console.WriteLine(@"Invalid output filename: " + outputFilename);
                    return;
                }
                throw;
            } 
            #endregion

            finally
            {
                long? length = output?.Length;
                output?.Close();
                if (length.HasValue && length.Value < 4)
                    File.Delete(outputFilename);
            }
            Console.Read();
        }
    }
}
