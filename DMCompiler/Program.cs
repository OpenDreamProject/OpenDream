using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DMCompiler.Compiler.DM;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Objects;

namespace DMCompiler {
    class Program {
        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("Two arguments are required:");
                Console.WriteLine("\tDM File");
                Console.WriteLine("\t\tPath to the DM file to be compiled");
                Console.WriteLine("\tOutput File");
                Console.WriteLine("\t\tPath to the output file");

                return;
            }

            string source = File.ReadAllText("DMStandard\\Standard.dm") + "\n" + File.ReadAllText(args[0]);
            DMLexer dmLexer = new DMLexer(source);
            DMParser dmParser = new DMParser(dmLexer);
            DMASTFile astFile = dmParser.File();
            DMASTSimplifier astSimplifier = new DMASTSimplifier();
            DMVisitorObjectBuilder dmObjectBuilder = new DMVisitorObjectBuilder();

            astSimplifier.SimplifyAST(astFile);
            Dictionary<DreamPath, DMObject> dmObjects = dmObjectBuilder.BuildObjects(astFile);
            DreamObjectJson objectTreeJson = CreateObjectTree(dmObjects);
            string json = JsonSerializer.Serialize(objectTreeJson, new JsonSerializerOptions() {
                IgnoreNullValues = true
            });

            File.WriteAllText(args[1], json);
        }

        private static DreamObjectJson CreateObjectTree(Dictionary<DreamPath, DMObject> dmObjects) {
            DreamObjectJson rootObject = CreateDreamObjectJson(dmObjects[DreamPath.Root]);
            dmObjects.Remove(DreamPath.Root);

            Dictionary<DreamPath, DreamObjectJson> jsonObjects = new Dictionary<DreamPath, DreamObjectJson>() { { DreamPath.Root, rootObject } };
            Queue<(DMObject, DreamObjectJson)> unparentedObjects = new Queue<(DMObject, DreamObjectJson)>();

            foreach (KeyValuePair<DreamPath, DMObject> dmObject in dmObjects) {
                DreamObjectJson jsonObject = CreateDreamObjectJson(dmObject.Value);

                jsonObjects.Add(dmObject.Key, jsonObject);
                unparentedObjects.Enqueue((dmObject.Value, jsonObject));
            }

            while (unparentedObjects.Count > 0) {
                (DMObject, DreamObjectJson) unparentedObject = unparentedObjects.Dequeue();
                DreamPath treeParentPath = unparentedObject.Item1.Path.FromElements(0, -2);

                if (jsonObjects.TryGetValue(treeParentPath, out DreamObjectJson treeParent)) {
                    if (treeParent.Children == null) treeParent.Children = new List<DreamObjectJson>();

                    treeParent.Children.Add(unparentedObject.Item2);
                    if (unparentedObject.Item1.Parent != null && unparentedObject.Item1.Parent.Value.Equals(treeParentPath)) {
                        unparentedObject.Item2.Parent = null; //Parent type can be assumed
                    }
                } else {
                    throw new Exception("Invalid object path \"" + unparentedObject.Item1.Path + "\"");
                }
            }

            AddNativeProcs(rootObject);
            return rootObject;
        }

        private static DreamObjectJson CreateDreamObjectJson(DMObject dmObject) {
            DreamObjectJson objectJson = new DreamObjectJson();

            objectJson.Name = (!dmObject.Path.Equals(DreamPath.Root)) ? dmObject.Path.LastElement : "";
            objectJson.Parent = dmObject.Parent?.PathString;

            if (dmObject.Variables.Count > 0) {
                objectJson.Variables = new Dictionary<string, object>();

                foreach (KeyValuePair<string, object> variable in dmObject.Variables) {
                    objectJson.Variables.Add(variable.Key, CreateDreamObjectJsonVariable(variable.Value));
                }
            }

            if (dmObject.GlobalVariables.Count > 0) {
                objectJson.GlobalVariables = new Dictionary<string, object>();

                foreach (KeyValuePair<string, object> variable in dmObject.GlobalVariables) {
                    objectJson.GlobalVariables.Add(variable.Key, CreateDreamObjectJsonVariable(variable.Value));
                }
            }

            if (dmObject.Procs.Count > 0) {
                objectJson.Procs = new Dictionary<string, List<ProcDefinitionJson>>();

                foreach (KeyValuePair<string, List<DMProc>> procs in dmObject.Procs) {
                    List<ProcDefinitionJson> procJson = new List<ProcDefinitionJson>();

                    foreach (DMProc proc in procs.Value) {
                        ProcDefinitionJson procDefinition = new ProcDefinitionJson();

                        if (proc.Bytecode.Length > 0) procDefinition.Bytecode = proc.Bytecode.ToArray();
                        if (proc.Parameters.Count > 0) {
                            procDefinition.ArgumentNames = new List<string>();

                            foreach (DMProc.Parameter parameter in proc.Parameters) {
                                procDefinition.ArgumentNames.Add(parameter.Name);

                                if (parameter.DefaultValue != null) {
                                    if (procDefinition.DefaultArgumentValues == null) procDefinition.DefaultArgumentValues = new Dictionary<string, object>();

                                    procDefinition.DefaultArgumentValues.Add(parameter.Name, CreateDreamObjectJsonVariable(parameter.DefaultValue));
                                }
                            }
                        }
                        procJson.Add(procDefinition);
                    }

                    objectJson.Procs.Add(procs.Key, procJson);
                }
            }

            return objectJson;
        }

        private static object CreateDreamObjectJsonVariable(object value) {
            if (value is int || value is float || value is string) {
                return value;
            } else if (value is null) {
                return new Dictionary<string, object>() {
                    { "type", DreamObjectJsonVariableType.Object }
                };
            } else if (value is DMList) {
                DMList dmList = (DMList)value;
                List<object> dmListValues = new List<object>();
                Dictionary<string, object> jsonVariable = new Dictionary<string, object>() {
                    { "type", DreamObjectJsonVariableType.List }
                };

                foreach (object dmListValue in dmList.Values) {
                    dmListValues.Add(new Dictionary<string, object>() {
                        { "value",  CreateDreamObjectJsonVariable(dmListValue) }
                    });
                }

                foreach (KeyValuePair<object, object> dmAssociatedListValue in dmList.AssociatedValues) {
                    object key;
                    if (dmAssociatedListValue.Key is DMResource) {
                        key = CreateDreamObjectJsonVariable(dmAssociatedListValue.Key);
                    } else if (dmAssociatedListValue.Key is string) {
                        key = dmAssociatedListValue.Key;
                    } else {
                        throw new Exception("Invalid list index");
                    }

                    dmListValues.Add(new Dictionary<string, object>() {
                        { "key", key },
                        { "value",  CreateDreamObjectJsonVariable(dmAssociatedListValue.Value) }
                    });
                }

                if (dmListValues.Count > 0) jsonVariable.Add("values", dmListValues);
                return jsonVariable;
            } else if (value is DMNewInstance) {
                return new Dictionary<string, object>() {
                    { "type", DreamObjectJsonVariableType.Object },
                    { "path", ((DMNewInstance)value).Path.PathString }
                };
            } else if (value is DMResource) {
                return new Dictionary<string, object>() {
                    { "type", DreamObjectJsonVariableType.Resource },
                    { "resourcePath", ((DMResource)value).ResourcePath }
                };
            } else if (value is DreamPath) {
                return new Dictionary<string, object>() {
                    { "type", DreamObjectJsonVariableType.Path },
                    { "value", ((DreamPath)value).PathString }
                };
            } else {
                throw new Exception("Invalid variable value");
            }
        }

        private static void AddNativeProcs(DreamObjectJson rootObject) {
            AddNativeProc(rootObject, "abs", "abs");
            AddNativeProc(rootObject, "animate", "animate");
            AddNativeProc(rootObject, "ascii2text", "ascii2text");
            AddNativeProc(rootObject, "ckey", "ckey");
            AddNativeProc(rootObject, "copytext", "copytext");
            AddNativeProc(rootObject, "CRASH", "CRASH");
            AddNativeProc(rootObject, "fexists", "fexists");
            AddNativeProc(rootObject, "file", "file");
            AddNativeProc(rootObject, "file2text", "file2text");
            AddNativeProc(rootObject, "findlasttext", "findlasttext");
            AddNativeProc(rootObject, "findtext", "findtext");
            AddNativeProc(rootObject, "findtextEx", "findtextEx");
            AddNativeProc(rootObject, "get_dir", "get_dir");
            AddNativeProc(rootObject, "get_dist", "get_dist");
            AddNativeProc(rootObject, "html_decode", "html_decode");
            AddNativeProc(rootObject, "html_encode", "html_encode");
            AddNativeProc(rootObject, "image", "image");
            AddNativeProc(rootObject, "isarea", "isarea");
            AddNativeProc(rootObject, "isloc", "isloc");
            AddNativeProc(rootObject, "ismob", "ismob");
            AddNativeProc(rootObject, "isnull", "isnull");
            AddNativeProc(rootObject, "isnum", "isnum");
            AddNativeProc(rootObject, "ispath", "ispath");
            AddNativeProc(rootObject, "istext", "istext");
            AddNativeProc(rootObject, "istype", "istype");
            AddNativeProc(rootObject, "isturf", "isturf");
            AddNativeProc(rootObject, "json_decode", "json_decode");
            AddNativeProc(rootObject, "json_encode", "json_encode");
            AddNativeProc(rootObject, "length", "length");
            AddNativeProc(rootObject, "locate", "locate");
            AddNativeProc(rootObject, "log", "log");
            AddNativeProc(rootObject, "lowertext", "lowertext");
            AddNativeProc(rootObject, "max", "max");
            AddNativeProc(rootObject, "min", "min");
            AddNativeProc(rootObject, "num2text", "num2text");
            AddNativeProc(rootObject, "orange", "orange");
            AddNativeProc(rootObject, "params2list", "params2list");
            AddNativeProc(rootObject, "pick", "pick");
            AddNativeProc(rootObject, "prob", "prob");
            AddNativeProc(rootObject, "replacetext", "replacetext");
            AddNativeProc(rootObject, "rand", "rand");
            AddNativeProc(rootObject, "round", "round");
            AddNativeProc(rootObject, "splittext", "splittext");
            AddNativeProc(rootObject, "sleep", "sleep");
            AddNativeProc(rootObject, "sound", "sound");
            AddNativeProc(rootObject, "text", "text");
            AddNativeProc(rootObject, "text2ascii", "text2ascii");
            AddNativeProc(rootObject, "text2num", "text2num");
            AddNativeProc(rootObject, "text2path", "text2path");
            AddNativeProc(rootObject, "time2text", "time2text");
            AddNativeProc(rootObject, "typesof", "typesof");
            AddNativeProc(rootObject, "uppertext", "uppertext");
            AddNativeProc(rootObject, "url_encode", "url_encode");
            AddNativeProc(rootObject, "view", "view");
            AddNativeProc(rootObject, "viewers", "viewers");
            AddNativeProc(rootObject, "walk", "walk");
            AddNativeProc(rootObject, "walk_to", "walk_to");

            DreamObjectJson listObject = rootObject.Children.Find((DreamObjectJson child) => {
                return child.Name == "list";
            });
            AddNativeProc(listObject, "Add", "list_Add");
            AddNativeProc(listObject, "Copy", "list_Copy");
            AddNativeProc(listObject, "Cut", "list_Cut");
            AddNativeProc(listObject, "Find", "list_Find");
            AddNativeProc(listObject, "Insert", "list_Insert");
            AddNativeProc(listObject, "Join", "list_Join");
            AddNativeProc(listObject, "Remove", "list_Remove");
            AddNativeProc(listObject, "Swap", "list_Swap");
        }

        private static void AddNativeProc(DreamObjectJson objectJson, string procName, string nativeProcName) {
            if (objectJson.Procs == null) objectJson.Procs = new Dictionary<string, List<ProcDefinitionJson>>();

            objectJson.Procs[procName] = new List<ProcDefinitionJson>() {
                new ProcDefinitionJson() {
                    NativeProcName = nativeProcName
                }
            };
        }
    }
}
