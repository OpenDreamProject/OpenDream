using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;
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

            string source = File.ReadAllText("DM\\Standard.dm") + "\n" + File.ReadAllText(args[0]);
            DMLexer dmLexer = new DMLexer(source);
            DMParser dmParser = new DMParser(dmLexer);
            DMASTFile astFile = dmParser.File();
            DMVisitorObjectBuilder dmObjectBuilder = new DMVisitorObjectBuilder();
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

            //TODO: Move to Standard.dm
            rootObject.GlobalVariables = new Dictionary<string, object>();
            rootObject.GlobalVariables.Add("world", new Dictionary<string, object>() {
                { "type", DreamObjectJsonVariableType.Object }
            });

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

            if (dmObject.Procs.Count > 0) {
                objectJson.Procs = new Dictionary<string, List<ProcDefinitionJson>>();

                foreach (KeyValuePair<string, List<DMProc>> procs in dmObject.Procs) {
                    List<ProcDefinitionJson> procJson = new List<ProcDefinitionJson>();

                    foreach (DMProc proc in procs.Value) {
                        procJson.Add(new ProcDefinitionJson());
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
                Dictionary<string, object> jsonVariable = new Dictionary<string, object>() {
                    { "type", DreamObjectJsonVariableType.Object },
                    { "path", "/list" }
                };

                if (dmList.Values.Length > 0) {
                    List<object> dmListValues = new List<object>();

                    foreach (object dmListValue in dmList.Values) {
                        dmListValues.Add(CreateDreamObjectJsonVariable(dmListValue));
                    }

                    jsonVariable.Add("arguments", dmListValues);
                }

                return jsonVariable;
            } else if (value is DMResource) {
                return new Dictionary<string, object>() {
                    { "type", DreamObjectJsonVariableType.Resource },
                    { "resourcePath", ((DMResource)value).ResourcePath }
                };
            } else {
                throw new Exception("Invalid variable value");
            }
        }
    }
}
