
using System;
namespace Content.Compiler.DM
{
    public partial class DMAST
    {
        internal static void PrintNode(DMASTNode n,  int depth)
        {
            var id = n as DMASTIdentifier;
            var callable_id = n as DMASTCallableProcIdentifier;
            var pad = new String(' ', 2 * depth);
            if (id != null)
            {
                Console.WriteLine(pad + ":" + n.GetType().Name + " " + id.Identifier);
            }
            else if (callable_id != null)
            {
                Console.WriteLine(pad + ":" + n.GetType().Name + " " + callable_id.Identifier);
            }
            else
            {
                Console.WriteLine(pad + ":" + n.GetType().Name);
            }
            foreach (var leaf in n.LeafNodes())
            {
                PrintNode(leaf, depth + 1);
            }
        }
        public static void PrintNodes(DMASTNode n)
        {
            PrintNode(n, 0);
        }
    }

}
