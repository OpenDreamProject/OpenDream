using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace OpenDreamServer.Dream {
    class DreamMap {
        public UInt16[,] Turfs { get; private set; }
        public int Width { get => Turfs.GetLength(0); }
        public int Height { get => Turfs.GetLength(1); }

        public void LoadMap(DreamResource mapResource) {
            string dmmSource = mapResource.ReadAsString();
            DMMParser.ParsedDMMMap parsedDMMMap = DMMParser.ParseDMMMap(dmmSource);

            Turfs = new UInt16[parsedDMMMap.Width, parsedDMMMap.Height];
            foreach (KeyValuePair<(int, int, int), string[,]> coordinateAssignment in parsedDMMMap.CoordinateAssignments) {
                (int, int, int) coordinates = coordinateAssignment.Key;
                string[,] typeNames = coordinateAssignment.Value;

                for (int x = 0; x < typeNames.GetLength(0); x++) {
                    for (int y = 0; y < typeNames.GetLength(1); y++) {
                        DMMParser.ParsedDMMType parsedDMMType = parsedDMMMap.Types[typeNames[x, y]];
                        DreamObject turf = CreateParsedDMMObject(parsedDMMType.Turf);

                        if (turf == null) turf = Program.DreamObjectTree.CreateObject(DreamPath.Turf);

                        SetTurf(x + 1, y + 1, turf);
                        foreach (DMMParser.ParsedDMMObject parsedMovable in parsedDMMType.Movables) {
                            CreateParsedDMMObject(parsedMovable, turf);
                        }
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

        private DreamObject CreateParsedDMMObject(DMMParser.ParsedDMMObject parsedDMMObject, DreamObject location = null) {
            if (!Program.DreamObjectTree.HasTreeEntry(parsedDMMObject.ObjectType)) return null;
            DreamObject createdObject = Program.DreamObjectTree.CreateObject(parsedDMMObject.ObjectType, new DreamProcArguments(new List<DreamValue>() { new DreamValue(location) }));

            return createdObject;
        }
    }
}
