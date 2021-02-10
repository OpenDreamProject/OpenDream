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
using OpenDreamShared.Json;

namespace DMCompiler {
    class Program {
        public static List<string> StringTable = new();
        public static Dictionary<string, int> StringToStringID = new();

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

            if (dmObject.Procs.Count > 0) {
                objectJson.Procs = new Dictionary<string, List<ProcDefinitionJson>>();

                foreach (KeyValuePair<string, List<DMProc>> procs in dmObject.Procs) {
                    List<ProcDefinitionJson> procJson = new List<ProcDefinitionJson>();

                    foreach (DMProc proc in procs.Value) {
                        ProcDefinitionJson procDefinition = new ProcDefinitionJson();

                        if (proc.Bytecode.Length > 0) procDefinition.Bytecode = proc.Bytecode.ToArray();
                        if (proc.Parameters.Count > 0) procDefinition.ArgumentNames = proc.Parameters;
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
                    { "type", JsonVariableType.Object }
                };
            } else if (value is DMList dmList) {
                List<object> dmListValues = new List<object>();
                Dictionary<string, object> jsonVariable = new Dictionary<string, object>() {
                    { "type", JsonVariableType.List }
                };

                foreach (object dmListValue in dmList.Values) {
                    dmListValues.Add(new Dictionary<string, object>() {
                        { "value",  CreateDreamObjectJsonVariable(dmListValue) }
                    });
                }

                foreach (KeyValuePair<object, object> dmAssociatedListValue in dmList.AssociatedValues) {
                    object key;
                    if (dmAssociatedListValue.Key is DMResource || dmAssociatedListValue.Key is DreamPath) {
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
            } else if (value is DMNewInstance newInstance) {
                return new Dictionary<string, object>() {
                    { "type", JsonVariableType.Object },
                    { "path", newInstance.Path.PathString }
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
