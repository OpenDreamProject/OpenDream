using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Resources;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Compiler.DMM;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamServer.Dream {
    class DreamMap {
        public UInt16[,] Turfs { get; private set; }
        public int Width { get => Turfs.GetLength(0); }
        public int Height { get => Turfs.GetLength(1); }

        public void LoadMap(DreamResource mapResource) {
            string dmmSource = mapResource.ReadAsString();
            DMMParser dmmParser = new DMMParser(new DMLexer(dmmSource));
            DMMParser.Map map = dmmParser.ParseMap();

            Turfs = new UInt16[map.MaxX - 1, map.MaxY - 1];
            foreach (DMMParser.MapBlock mapBlock in map.Blocks) {
                foreach (KeyValuePair<(int X, int Y), string> cell in mapBlock.Cells) {
                    DMMParser.CellDefinition cellDefinition = map.CellDefinitions[cell.Value];
                    DreamObject turf = CreateMapObject(cellDefinition.Turf);
                    if (turf == null) turf = Program.DreamObjectTree.CreateObject(DreamPath.Turf);

                    SetTurf(mapBlock.X + cell.Key.X - 1, mapBlock.Y + cell.Key.Y - 1, turf);
                    foreach (DMMParser.MapObject mapObject in cellDefinition.Objects) {
                        CreateMapObject(mapObject, turf);
                    }
                }
            }
        }

        public void SetTurf(int x, int y, DreamObject turf) {
            if (!turf.IsSubtypeOf(DreamPath.Turf)) {
                throw new Exception("Turf was not a sub-type of " + DreamPath.Turf);
            }

            SetTurfUnsafe(x, y, DreamMetaObjectAtom.AtomIDs[turf]);
        }

        public bool IsTurfOnMap(DreamObject turf) {
            if (!turf.IsSubtypeOf(DreamPath.Turf)) {
                throw new Exception("Turf is not a sub-type of " + DreamPath.Turf);
            }

            UInt16 turfAtomID = DreamMetaObjectAtom.AtomIDs[turf];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    if (Turfs[x, y] == turfAtomID) return true;
                }
            }

            return false;
        }

        public Point GetTurfLocation(DreamObject turf) {
            if (!turf.IsSubtypeOf(DreamPath.Turf)) {
                throw new Exception("Turf is not a sub-type of " + DreamPath.Turf);
            }

            UInt16 turfAtomID = DreamMetaObjectAtom.AtomIDs[turf];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    if (Turfs[x, y] == turfAtomID) return new Point(x + 1, y + 1);
                }
            }

            throw new Exception("Turf does not have a location");
        }

        public DreamObject GetTurfAt(int x, int y) {
            return DreamMetaObjectAtom.AtomIDToAtom[Turfs[x - 1, y - 1]];
        }

        private void SetTurfUnsafe(int x, int y, UInt16 turfAtomID) {
            Turfs[x - 1, y - 1] = turfAtomID;

            Program.DreamStateManager.AddTurfDelta(x - 1, y - 1, turfAtomID);
        }

        private DreamObject CreateMapObject(DMMParser.MapObject mapObject, DreamObject loc = null) {
            if (!Program.DreamObjectTree.HasTreeEntry(mapObject.Type)) return null;
            DreamObject dreamObject = Program.DreamObjectTree.CreateObject(mapObject.Type, new DreamProcArguments(new() { new DreamValue(loc) }));

            foreach (KeyValuePair<string, DreamValue> varOverride in mapObject.VarOverrides) {
                if (dreamObject.HasVariable(varOverride.Key)) {
                    dreamObject.SetVariable(varOverride.Key, varOverride.Value);
                }
            }

            return dreamObject;
        }
    }
}
