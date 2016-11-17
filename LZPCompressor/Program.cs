﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //List<byte[]> badArrs = new List<byte[]>();
            //for (int i = 0; i < int.MaxValue; i++)
            //{
            //    Random rnd = new Random();
            //    int length = rnd.Next(4, 5000);
            //    byte[] arr = new byte[length];
            //    rnd.NextBytes(arr);
            //    byte[] arr2 = new LZP1().Decompress(new LZP1().Compress(arr));
            //    if (arr.Length != arr2.Length)
            //    {
            //        Console.WriteLine("Error length");
            //        badArrs.Add(arr);
            //    }
            //    else
            //        for (int j = 0; j < arr.Length; j++)
            //            if (arr[j] != arr2[j])
            //            {
            //                Console.WriteLine("Error bytes");
            //                badArrs.Add(arr);
            //                break;
            //            }
            //}
            //Console.WriteLine(badArrs.Count);
            //for (int i = 1; i <= badArrs.Count; i++)
            //{
            //    File.WriteAllBytes("error" + i, badArrs[i - 1]);
            //}
            //Console.ReadLine();


            string file = args[0];
            var input = File.ReadAllBytes(file);
            Console.WriteLine("Input size: " + input.Length);
            var sw = Stopwatch.StartNew();
            var output = new LZP1().Compress(input);
            sw.Stop();
            Console.WriteLine("Compress time: " + sw.ElapsedMilliseconds);
            Console.WriteLine("Output size: " + output.Length);
            sw = Stopwatch.StartNew();
            new LZP1().Decompress(output);
            sw.Stop();
            Console.WriteLine("Decompress time: " + sw.ElapsedMilliseconds);

            Console.ReadLine();

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
        }
    }
}
