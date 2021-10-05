
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenDreamShared.Dream;

namespace DMCompiler.Compiler.DM {


    public static partial class DMAST {
        private static List<Type> printable_field_types = new() { typeof(string), typeof(DreamPath), typeof(int), typeof(float) };
        internal static StringBuilder PrintNode(DMASTNode n, int depth, int max_depth = -1) {
            StringBuilder sb = new();
            if (max_depth == 0) {
                return sb;
            }
            var pad = new String(' ', 2 * depth);
            if (n == null) {
                sb.Append("null");
                return sb;
            }
            sb.Append(pad + n.GetType().Name + ": ");
            var new_max_depth = max_depth - 1;
            foreach (var field in n.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
                if (printable_field_types.Contains(field.FieldType)) {
                    sb.Append(field.Name + "=" + field.GetValue(n) + " ");
                }
            }
            sb.Append('\n');
            foreach (var leaf in n.LeafNodes()) {
                sb.Append(PrintNode(leaf, depth + 1, new_max_depth));
            }
            return sb;
        }
        public static string PrintNodes(this DMASTNode n, int depth = 0, int max_depth = -1) {
            return PrintNode(n, depth, max_depth).ToString();
        }

        public delegate void CompareResult(DMASTNode n_l, DMASTNode n_r, string s);

        public static bool Compare(DMASTNode node_l, DMASTNode node_r, CompareResult cr) {
            if (node_l == null || node_r == null) {
                if (node_r == node_l) { return true; }
                cr(node_l, node_r, "null mismatch");
                return false;
            }

            if (node_l.GetType() != node_r.GetType()) { cr(node_l, node_r, "type mismatch"); return false; }

            List<object> compared = new();
            DMASTNode[] subnodes_l = node_l.LeafNodes().ToArray();
            DMASTNode[] subnodes_r = node_r.LeafNodes().ToArray();

            if (subnodes_l.Length != subnodes_r.Length) { cr(node_l, node_r, "nodes length mismatch " + subnodes_l.Length + " " + subnodes_r.Length); return false; }

            for (var i = 0; i < subnodes_l.Length; i++) {
                Compare(subnodes_l[i], subnodes_r[i], cr);
                compared.Add(subnodes_l);
            }

            foreach (var field in node_l.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
                if (compared.Contains(field.GetValue(node_l))) {
                    continue;
                }
                //TODO non-node type field checking goes here
            }

            return true;
        }
        public static IEnumerable<DMASTNode> LeafNodes(this DMASTNode node) {
            foreach (var field in node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) {
                var value = field.GetValue(node);
                if (value == null) { continue; }
                if (field.FieldType.IsAssignableTo(typeof(DMASTNode))) {
                    yield return value as DMASTNode;
                }
                else if (field.FieldType.IsArray && field.FieldType.GetElementType().IsAssignableTo(typeof(DMASTNode))) {
                    var field_value = value as DMASTNode[];
                    foreach (var subnode in field_value) {
                        yield return subnode;
                    }
                }
            }
        }
    }
}
