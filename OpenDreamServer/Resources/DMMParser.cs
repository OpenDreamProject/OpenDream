﻿using OpenDreamServer.Dream;
using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace OpenDreamServer.Resources {
    static class DMMParser {
        public struct ParsedDMMMap {
            public int TypeNameWidth;
            public Dictionary<string, ParsedDMMType> Types;
            public Dictionary<(int, int, int), string[,]> CoordinateAssignments;
            public int Width, Height;
        }

        public struct ParsedDMMType {
            public ParsedDMMObject Area;
            public ParsedDMMObject Turf;
            public List<ParsedDMMObject> Movables;
        }

        public struct ParsedDMMObject {
            public DreamPath ObjectType;
            public Dictionary<string, DreamValue> VariableModifications;
        }

        public static ParsedDMMMap ParseDMMMap(string dmfSource) {
            ParsedDMMMap parsedDMMMap = new ParsedDMMMap();
            parsedDMMMap.TypeNameWidth = 1;
            parsedDMMMap.Types = new Dictionary<string, ParsedDMMType>();
            parsedDMMMap.CoordinateAssignments = new Dictionary<(int, int, int), string[,]>();

            dmfSource = dmfSource.Replace(" ", "");

            string[] lines = dmfSource.Split("\n");
            List<string> dmmLines = new List<string>();
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
                string dmmLine = lines[lineIndex].Trim();

                if (dmmLine.EndsWith("(")) {
                    while (lines[lineIndex].IndexOf(")") == -1 && lineIndex < lines.Length) {
                        lineIndex++;
                        dmmLine += lines[lineIndex].Trim();
                    }
                } else if (dmmLine.EndsWith("{") || dmmLine.EndsWith("{\"")) {
                    while (lines[lineIndex].IndexOf("}") == -1 && lineIndex < lines.Length) {
                        lineIndex++;
                        dmmLine += lines[lineIndex].Trim();
                    }
                }

                dmmLines.Add(dmmLine);
            }

            foreach (string line in dmmLines) {
                int equalsIndex = line.IndexOf("=");

                if (equalsIndex != -1) {
                    string key = line.Substring(0, equalsIndex);
                    string value = line.Substring(equalsIndex + 1);

                    if (key.StartsWith("\"") && key.EndsWith("\"")) {
                        ParsedDMMType parsedDMMType = new ParsedDMMType();
                        string typeName = key.Substring(1, key.Length - 2);

                        parsedDMMMap.TypeNameWidth = typeName.Length;
                        parsedDMMType.Movables = new List<ParsedDMMObject>();
                        if (value.StartsWith("(") && value.EndsWith(")")) {
                            string[] dmmObjects = value.Substring(1, value.Length - 2).Split(",");

                            foreach (string dmmObject in dmmObjects) {
                                ParsedDMMObject parsedDMMObject = new ParsedDMMObject();
                                int varModificationsIndex = dmmObject.IndexOf("{");

                                if (varModificationsIndex != -1) {
                                    string objectType = dmmObject.Substring(0, varModificationsIndex);
                                    string[] varModifications = dmmObject.Substring(varModificationsIndex + 1, dmmObject.Length - varModificationsIndex - 2).Split(";");

                                    parsedDMMObject.ObjectType = new DreamPath(objectType);
                                    parsedDMMObject.VariableModifications = new Dictionary<string, DreamValue>();
                                    foreach (string varModification in varModifications) {
                                        string[] varModificationSplit = varModification.Split("=");
                                        if (varModificationSplit.Length != 2) continue;
                                        string varName = varModificationSplit[0];
                                        string varValue = varModificationSplit[1];

                                        if (int.TryParse(varValue, out int varValueInteger)) {
                                            parsedDMMObject.VariableModifications[varName] = new DreamValue(varValueInteger);
                                        } else if (double.TryParse(varValue, out double varValueDouble)) {
                                            parsedDMMObject.VariableModifications[varName] = new DreamValue(varValueDouble);
                                        } else if (varValue.StartsWith("\"") && varValue.EndsWith("\"") || varValue.StartsWith("'") && varValue.EndsWith("'")) {
                                            parsedDMMObject.VariableModifications[varName] = new DreamValue(varValue.Substring(1, varValue.Length - 2));
                                        } else if (varValue.StartsWith("/")) {
                                            parsedDMMObject.VariableModifications[varName] = new DreamValue(new DreamPath(varValue));
                                        } else if (varValue == "null") {
                                            parsedDMMObject.VariableModifications[varName] = new DreamValue((DreamObject)null);
                                        } else {
                                            throw new Exception("Invalid var modification value (" + varModification + ")");
                                        }
                                    }
                                } else {
                                    parsedDMMObject.ObjectType = new DreamPath(dmmObject);
                                    parsedDMMObject.VariableModifications = null;
                                }

                                if (parsedDMMObject.ObjectType.IsDescendantOf(DreamPath.Area)) {
                                    parsedDMMType.Area = parsedDMMObject;
                                } else if (parsedDMMObject.ObjectType.IsDescendantOf(DreamPath.Turf)) {
                                    parsedDMMType.Turf = parsedDMMObject;
                                } else {
                                    parsedDMMType.Movables.Add(parsedDMMObject);
                                }
                            }

                        } else {
                            throw new Exception("Invalid value \"" + value + "\"");
                        }

                        parsedDMMMap.Types[typeName] = parsedDMMType;
                    } else if (key.StartsWith("(") && key.EndsWith(")")) {
                        string[] coordinates = key.Substring(1, key.Length - 2).Split(",");
                        int x = int.Parse(coordinates[0]);
                        int y = int.Parse(coordinates[1]);
                        int z = int.Parse(coordinates[2]);

                        if (value.StartsWith("{\"") && value.EndsWith("\"}")) {
                            string coordinateDefinition = value.Substring(2, value.Length - 4);

                            if (coordinateDefinition.Length % parsedDMMMap.TypeNameWidth == 0) {
                                int mapSize = (int)Math.Ceiling(Math.Sqrt(coordinateDefinition.Length / parsedDMMMap.TypeNameWidth));

                                parsedDMMMap.CoordinateAssignments[(x, y, z)] = new string[mapSize, mapSize];
                                if (parsedDMMMap.Width < x + mapSize) parsedDMMMap.Width = x + mapSize;
                                if (parsedDMMMap.Height < y + mapSize) parsedDMMMap.Height = y + mapSize;

                                for (int i = 0; i < coordinateDefinition.Length / parsedDMMMap.TypeNameWidth; i++) {
                                    string typeName = coordinateDefinition.Substring((i * parsedDMMMap.TypeNameWidth), parsedDMMMap.TypeNameWidth);

                                    parsedDMMMap.CoordinateAssignments[(x, y, z)][i % mapSize, i / mapSize] = typeName;
                                }
                            } else {
                                throw new Exception("Invalid coordinate definition length");
                            }
                        } else {
                            throw new Exception("Invalid value \"" + value + "\"");
                        }
                    } else {
                        throw new Exception("Invalid key (" + key + ")");
                    }
                }
            }

            return parsedDMMMap;
        }
    }
}
