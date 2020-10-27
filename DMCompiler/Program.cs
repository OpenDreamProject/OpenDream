using System;
using System.IO;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;

namespace DMCompiler {
    class Program {
        static void Main(string[] args) {
            if (args.Length < 1) {
                Console.WriteLine("One argument is required:");
                Console.WriteLine("\tDM File");
                Console.WriteLine("\t\tPath to the DM file to be compiled");

                return;
            }

            string source = File.ReadAllText(args[0]);
            DMLexer dmLexer = new DMLexer(source);
            DMParser dmParser = new DMParser(dmLexer);
            DMVisitorPrint dmPrinter = new DMVisitorPrint();

            dmParser.File().Visit(dmPrinter);
        }
    }
}
