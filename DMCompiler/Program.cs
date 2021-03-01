using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DMCompiler.Compiler.DM;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;
using DMCompiler.Preprocessor;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Json;

namespace DMCompiler {
    class Program {
        public static List<string> StringTable = new();
        public static Dictionary<string, int> StringToStringID = new();
        public static DMProc GlobalInitProc = new();

        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("Three arguments are required:");
                Console.WriteLine("\tInclude Path");
                Console.WriteLine("\t\tPath to the folder containing the code");
                Console.WriteLine("\tDME File");
                Console.WriteLine("\t\tPath to the DME file to be compiled");
                Console.WriteLine("\tOutput File");
                Console.WriteLine("\t\tPath to the output file");

                return;
            }

            DMPreprocessor preprocessor = new DMPreprocessor();
            preprocessor.IncludeFile("DMStandard", "_Standard.dm");
            preprocessor.IncludeFile(args[0], args[1]);

            string source = preprocessor.GetResult();
            DMLexer dmLexer = new DMLexer(source);
            DMParser dmParser = new DMParser(dmLexer);
            DMASTFile astFile = dmParser.File();
            
            DMASTSimplifier astSimplifier = new DMASTSimplifier();
            astSimplifier.SimplifyAST(astFile);

            DMVisitorObjectBuilder dmObjectBuilder = new DMVisitorObjectBuilder();
            Dictionary<DreamPath, DMObject> dmObjects = dmObjectBuilder.BuildObjects(astFile);

            DreamCompiledJson compiledDream = new DreamCompiledJson();
            compiledDream.Strings = StringTable;
            compiledDream.GlobalInitProc = new ProcDefinitionJson() { Bytecode = GlobalInitProc.Bytecode.ToArray() };
            compiledDream.RootObject = CreateObjectTree(dmObjects);

            string json = JsonSerializer.Serialize(compiledDream, new JsonSerializerOptions() {
                IgnoreNullValues = true
            });
            
            File.WriteAllText(args[2], json);
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

            return rootObject;
        }

        private static DreamObjectJson CreateDreamObjectJson(DMObject dmObject) {
            DreamObjectJson objectJson = new DreamObjectJson();

            objectJson.Name = (!dmObject.Path.Equals(DreamPath.Root)) ? dmObject.Path.LastElement : "";
            objectJson.Parent = dmObject.Parent?.PathString;

            if (dmObject.Variables.Count > 0) {
                objectJson.Variables = new Dictionary<string, object>();

                foreach (KeyValuePair<string, DMVariable> variable in dmObject.Variables) {
                    objectJson.Variables.Add(variable.Key, CreateDreamObjectJsonVariable(variable.Value.Value));
                }
            }

            if (dmObject.GlobalVariables.Count > 0) {
                objectJson.GlobalVariables = new Dictionary<string, object>();

                foreach (KeyValuePair<string, DMVariable> variable in dmObject.GlobalVariables) {
                    objectJson.GlobalVariables.Add(variable.Key, CreateDreamObjectJsonVariable(variable.Value.Value));
                }
            }

            if (dmObject.InitializationProc != null) {
                objectJson.InitProc = new ProcDefinitionJson() {
                    Bytecode = dmObject.InitializationProc.Bytecode.ToArray()
                };
            }

            if (dmObject.Procs.Count > 0) {
                objectJson.Procs = new Dictionary<string, List<ProcDefinitionJson>>();

                foreach (KeyValuePair<string, List<DMProc>> procs in dmObject.Procs) {
                    List<ProcDefinitionJson> procJson = new List<ProcDefinitionJson>();

                    foreach (DMProc proc in procs.Value) {
                        ProcDefinitionJson procDefinition = new ProcDefinitionJson();

                        if (proc.Bytecode.Length > 0) procDefinition.Bytecode = proc.Bytecode.ToArray();
                        if (proc.Parameters.Count > 0) {
                            procDefinition.Arguments = new List<ProcArgumentJson>();
                            
                            for (int i = 0; i < proc.Parameters.Count; i++) {
                                string argumentName = proc.Parameters[i];
                                DMValueType argumentType = proc.ParameterTypes[i];

                                procDefinition.Arguments.Add(new ProcArgumentJson() {
                                    Name = argumentName,
                                    Type = argumentType
                                });
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
                    { "type", JsonVariableType.Null }
                };
            } else if (value is DMResource resource) {
                return new Dictionary<string, object>() {
                    { "type", JsonVariableType.Resource },
                    { "resourcePath", resource.ResourcePath }
                };
            } else if (value is DreamPath path) {
                return new Dictionary<string, object>() {
                    { "type", JsonVariableType.Path },
                    { "value", path.PathString }
                };
            } else {
                throw new Exception("Invalid variable value");
            }
        }
    }
}
