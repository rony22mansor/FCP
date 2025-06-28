using FCP.Controllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FCP
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args));
        

        //byte[] fileData = File.ReadAllBytes("R:\\MultiMedia\\final project\\image.png");
        //foreach (var item in fileData)
        //{
        //    Console.WriteLine("fileData: " + (item));
        //}
        //Console.WriteLine("==============");
        //var huffman = new HuffmanAlgorithm();
        //var cd = huffman.Compress(fileData);
        //foreach (var item in cd)
        //{
        //    Console.WriteLine("cd: " + (item));
        //}
        //File.WriteAllBytes("R:\\MultiMedia\\final project\\arch.txt", cd);

        //Console.WriteLine("==============");

        //var undo_cd = huffman.Decompress(cd);
        //foreach (var item in undo_cd)
        //{
        //    Console.WriteLine("undo_cd: " + (item));
        //}

        //File.WriteAllBytes("R:\\MultiMedia\\final project\\image_decompressed.png", undo_cd);
    }
    }
}
