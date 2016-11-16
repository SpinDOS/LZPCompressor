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
            string file = args[0];
            var input = File.ReadAllBytes(file);
            Console.WriteLine("Last bytes: " + input[input.Length - 2] + " " + input[input.Length - 1]);
            Console.WriteLine("Input size: " + input.Length);
            var sw = Stopwatch.StartNew();
            var output = new LZP1().Compress(input);
            sw.Stop();
            Console.WriteLine("Time: " + sw.ElapsedMilliseconds);
            Console.WriteLine("Output size: " + output.Length);
            input = null;
            input = new LZP1().Decompress(output);
            Console.WriteLine("Decompressed size: " + input.Length);
            Console.ReadLine();

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
        }
    }
}
