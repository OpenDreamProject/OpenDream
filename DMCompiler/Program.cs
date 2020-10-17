using System;
using System.IO;

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
            Token dmToken;

            do {
                dmToken = dmLexer.GetNextToken();

                Console.WriteLine(dmToken.Type + ": " + ((dmToken.Type != TokenType.Newline) ? dmToken.Text : ""));
            } while (dmToken.Type != TokenType.EndOfFile);
        }
    }
}
