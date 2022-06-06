
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Compiler;

namespace DMCompiler.Compiler.DM {

    public static partial class DMAST {
        public class TopLevelTraveler {
            public Action<DMASTNode> VisitDefine;

            public void Travel(DMASTFile root) {
                Travel(root.BlockInner);
            }
            public void Travel(DMASTBlockInner block) {
                if (block == null) { return; }
                if (block.Statements != null) {
                    foreach (var stmt in block.Statements) {
                        Travel((dynamic)stmt);
                    }
                }
            }

            public void Travel(DMASTObjectDefinition objdef) {
                Travel(objdef.InnerBlock);
                VisitDefine(objdef);
            }

            public void Travel(DMASTObjectVarDefinition vardef) {
                VisitDefine(vardef);
            }
            public void Travel(DMASTObjectVarOverride vardef) {
                VisitDefine(vardef);
            }

            public void Travel(DMASTProcDefinition procdef) {
                VisitDefine(procdef);
            }
        }
        public class ASTHasher {
            public static string Hash(DMASTObjectDefinition objdef) {
                return $"OD-{objdef.Path}";
            }

            public static string Hash(DMASTObjectVarDefinition vardef) {
                return $"OVD-{vardef.ObjectPath}-{vardef.Name}";
            }
            public static string Hash(DMASTObjectVarOverride vardef) {
                return $"OVO-{vardef.ObjectPath}-{vardef.VarName}";
            }

            public static string Hash(DMASTProcDefinition procdef) {
                return $"PD-{procdef.ObjectPath}-{procdef.IsOverride}-{procdef.Name}";
            }

            public Dictionary<string, List<DMASTNode>> nodes = new();

            public List<DMASTNode> GetNode(DMASTNode node) {
                if (nodes.TryGetValue(Hash((dynamic)node), out List<DMASTNode> rval)) {
                    return rval;
                }
                else { return null; }
            }
            public void HashFile(DMASTFile node) {
                var traveler = new TopLevelTraveler();
                traveler.VisitDefine = HashDefine;
                traveler.Travel(node);
            }

            public DMASTProcDefinition GetProcByPath(string path) {
                var h = Hash(new DMASTProcDefinition(Location.Unknown, new OpenDreamShared.Dream.DreamPath(path), new DMASTDefinitionParameter[0], null));
                return nodes[h][0] as DMASTProcDefinition;
            }
            public void HashDefine(DMASTNode node) {
                var h = Hash((dynamic)node);
                if (nodes.ContainsKey(h)) {
                    nodes[h].Add(node);
                }
                else {
                    nodes.Add(h, new List<DMASTNode> { node });
                }
            }
        }

        public class Labeler {
            public Dictionary<object, int> labels = new();
            public int label_i = 0;

            public void Add(object obj) {
                if (labels.ContainsKey(obj)) {
                    return;
                }
                labels[obj] = label_i++;
            }

            public int? GetLabel(object obj) {
                if (labels.TryGetValue(obj, out var i)) {
                    return i;
                }
                return null;
            }

        }

        public class ObjectPrinter {
            public List<Type> tostring_types = new() {
                typeof(string),
                typeof(int),
                typeof(float),
                typeof(bool),
                typeof(char)
            };
            public List<Type> recurse_types = new() { };
            public List<Type> ignore_types = new() { };

            public class ObjectTraveler { }

            public void Print(object node, System.IO.TextWriter print, int depth = 0, int max_depth = 9999, Labeler labeler = null) {
                if (depth > max_depth) {
                    return;
                }
                string pad = new string(' ', 4 + 2 * depth);
                string line = "";
                if (node == null) {
                    print.WriteLine(pad + "null");
                    return;
                }
                else {
                    line += node.GetType().Name + " ";
                }
                List<(string, object)> recurse = new();

                if (node is string s) {
                    print.Write(Regex.Escape(node.ToString()));
                    return;
                }
                if (node.GetType().IsArray) {
                    var a = (Array)node;
                    var i = 0;
                    foreach (var e in a) {
                        recurse.Add((i.ToString(), e));
                        i += 1;
                    }
                }
                foreach (var field in node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) {
                    Type ty = Nullable.GetUnderlyingType(field.FieldType);
                    if (ty == null) {
                        ty = field.FieldType;
                    }
                    if (ignore_types.Contains(ty)) {
                        continue;
                    }

                    var v = field.GetValue(node);
                    bool is_recurse = false;
                    foreach (var rt in recurse_types) {
                        if (ty.IsAssignableTo(rt)) {
                            recurse.Add((field.Name, v));
                            is_recurse = true;
                            break;
                        }
                    }
                    if (is_recurse) {
                        continue;
                    }
                    if (ty.IsArray) {
                        recurse.Add((field.Name, v));
                    }
                    else if (v == null) {
                        line += field.Name + "=" + "null" + " ";
                    }
                    else if (tostring_types.Contains(ty)) {
                        line += field.Name + "=" + v.ToString() + " ";
                    }
                    else {
                        throw new Exception("unknown field type " + ty.ToString());
                    }
                }
                var label_i = labeler?.GetLabel(node);
                if (label_i != null) {
                    print.WriteLine(label_i.ToString().PadRight(4 + 2 * depth) + line);
                }
                else {
                    print.WriteLine("".PadRight(4 + 2 * depth) + line);
                }
                foreach (var r in recurse) {
                    if (r.Item2 != null) {
                        print.WriteLine(pad + "->" + r.Item1);
                        Print(r.Item2, print, depth + 1, max_depth, labeler);
                    }
                    else {
                        print.WriteLine(pad + "->" + r.Item1 + "=null");
                    }
                }
            }
        }

        // Example usage:
        // var hasher = new DMAST.ASTHasher();
        // hasher.HashFile(astFile);
        //    var proc = hasher.GetProcByPath("/datum/browser/proc/get_header");
        // new DMAST.DMASTNodePrinter().Print(proc, Console.Out);
        public class DMASTNodePrinter : ObjectPrinter {
            public DMASTNodePrinter() {
                tostring_types.AddRange( new Type[] { typeof(DMValueType), typeof(DreamPath), typeof(DreamPath.PathType) } );
                recurse_types.AddRange( new Type[] { typeof(DMASTDereference.DereferenceType), typeof(DMASTNode), typeof(DMASTCallable), typeof(VarDeclInfo) } );
                ignore_types.Add(typeof(Location));
            }
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
