using OpenDreamServer.Dream;
using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Dream.Procs.Native;
using OpenDreamServer.Net;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Interface;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDreamServer {
    class Program {
        public static DreamResourceManager DreamResourceManager = null;
        public static DreamStateManager DreamStateManager = new DreamStateManager();
        public static DreamObjectTree DreamObjectTree = new DreamObjectTree();
        public static DreamMap DreamMap = null;
        public static DreamServer DreamServer = new DreamServer(25566);
        public static DreamObject WorldInstance = null;
        public static Dictionary<DreamObjectDefinition, UInt16> AtomBaseIDs = new Dictionary<DreamObjectDefinition, UInt16>();
        public static Dictionary<DreamObject, DreamConnection> ClientToConnection = new Dictionary<DreamObject, DreamConnection>();
        public static int TickCount = 0;

        private static InterfaceDescriptor clientInterface = null;

        static void Main(string[] args) {
            if (args.Length < 3) {
                Console.WriteLine("Three arguments are required:");
                Console.WriteLine("\tResource Location");
                Console.WriteLine("\t\tPath to the folder holding all the game's assets");
                Console.WriteLine("\tObject Tree Location");
                Console.WriteLine("\t\tPath to the JSON file holding a compiled form of the game's code, relative to the resource location");
                Console.WriteLine("\tMap File Location");
                Console.WriteLine("\t\tPath to the map's DMM file, relative to the resource location");

                return;
            }

            string resourceLocation = args[0];
            string objectTreeFile = args[1];
            string mapFile = args[2];

            DreamResourceManager = new DreamResourceManager(resourceLocation);
            clientInterface = CreateClientInterface();

            DreamStateManager.DeltaStateFinalized += OnDeltaStateFinalized;
            DreamServer.DreamConnectionRequest += OnDreamConnectionRequest;

            DreamServer.RegisterPacketCallback<PacketRequestResource>(PacketID.RequestResource, DreamResourceManager.HandleRequestResourcePacket);
            DreamServer.RegisterPacketCallback<PacketKeyboardInput>(PacketID.KeyboardInput, (DreamConnection connection, PacketKeyboardInput pKeyboardInput) => {
                if (pKeyboardInput.KeysDown.Contains(38)) {
                    connection.ClientDreamObject?.CallProc("North");
                } else if (pKeyboardInput.KeysDown.Contains(39)) {
                    connection.ClientDreamObject?.CallProc("East");
                } else if (pKeyboardInput.KeysDown.Contains(40)) {
                    connection.ClientDreamObject?.CallProc("South");
                } else if (pKeyboardInput.KeysDown.Contains(37)) {
                    connection.ClientDreamObject?.CallProc("West");
                }
            });

            RegisterNativeProcs();
            DreamObjectTree.LoadFromJson(DreamResourceManager.LoadResource(objectTreeFile).ReadAsString());
            DreamObjectTree.SetMetaObject(DreamPath.Root, new DreamMetaObjectRoot());
            DreamObjectTree.SetMetaObject(DreamPath.List, new DreamMetaObjectList());
            DreamObjectTree.SetMetaObject(DreamPath.Sound, new DreamMetaObjectSound());
            DreamObjectTree.SetMetaObject(DreamPath.World, new DreamMetaObjectWorld());
            DreamObjectTree.SetMetaObject(DreamPath.Client, new DreamMetaObjectClient());
            DreamObjectTree.SetMetaObject(DreamPath.Datum, new DreamMetaObjectDatum());
            DreamObjectTree.SetMetaObject(DreamPath.Atom, new DreamMetaObjectAtom());
            DreamObjectTree.SetMetaObject(DreamPath.Turf, new DreamMetaObjectTurf());
            DreamObjectTree.SetMetaObject(DreamPath.Movable, new DreamMetaObjectMovable());
            DreamObjectTree.SetMetaObject(DreamPath.Mob, new DreamMetaObjectMob());
            CreateAtomBases();

            WorldInstance = DreamObjectTree.CreateObject(DreamPath.World, new DreamProcArguments(new List<DreamValue>()));
            DreamObjectTree.GetObjectDefinitionFromPath(DreamPath.Root).GlobalVariables["world"].Value = new DreamValue(WorldInstance);
            DreamObjectTree.InstantiateGlobalVariables();

            DreamMap = new DreamMap();
            DreamMap.LoadMap(DreamResourceManager.LoadResource(mapFile));

            Task.Run(() => WorldInstance.CallProc("New"));
            DreamServer.Start();
            while (true) {
                DreamStateManager.FinalizeCurrentDeltaState();
                DreamServer.Process();

                TickCount++;
                Thread.Sleep(100 * WorldInstance.GetVariable("tick_lag").GetValueAsInteger());
            }
        }

        private static InterfaceDescriptor CreateClientInterface() {
            InterfaceElementDescriptor mapwindowMain = new InterfaceElementDescriptor("mapwindow", InterfaceElementDescriptor.InterfaceElementDescriptorType.Main);
            mapwindowMain.CoordinateAttributes["pos"] = new Point(0, 0);
            mapwindowMain.DimensionAttributes["size"] = new Size(640, 480);
            mapwindowMain.BoolAttributes["is-pane"] = true;
            InterfaceElementDescriptor map = new InterfaceElementDescriptor("map", InterfaceElementDescriptor.InterfaceElementDescriptorType.Map);
            map.CoordinateAttributes["pos"] = new Point(0, 0);
            map.DimensionAttributes["size"] = new Size(640, 480);
            map.CoordinateAttributes["anchor1"] = new Point(0, 0);
            map.CoordinateAttributes["anchor2"] = new Point(100, 100);
            map.BoolAttributes["is-default"] = true;
            InterfaceWindowDescriptor mapwindow = new InterfaceWindowDescriptor("mapwindow", new List<InterfaceElementDescriptor>() { mapwindowMain, map });

            InterfaceElementDescriptor infowindowMain = new InterfaceElementDescriptor("infowindow", InterfaceElementDescriptor.InterfaceElementDescriptorType.Main);
            infowindowMain.CoordinateAttributes["pos"] = new Point(0, 0);
            infowindowMain.DimensionAttributes["size"] = new Size(640, 480);
            infowindowMain.BoolAttributes["is-pane"] = true;
            InterfaceElementDescriptor info = new InterfaceElementDescriptor("info", InterfaceElementDescriptor.InterfaceElementDescriptorType.Child);
            info.CoordinateAttributes["pos"] = new Point(0, 30);
            info.DimensionAttributes["size"] = new Size(640, 445);
            info.CoordinateAttributes["anchor1"] = new Point(0, 0);
            info.CoordinateAttributes["anchor2"] = new Point(100, 100);
            info.StringAttributes["left"] = "statwindow";
            info.StringAttributes["right"] = "outputwindow";
            info.BoolAttributes["is-vert"] = false;
            InterfaceWindowDescriptor infowindow = new InterfaceWindowDescriptor("infowindow", new List<InterfaceElementDescriptor>() { infowindowMain, info });

            InterfaceElementDescriptor outputwindowMain = new InterfaceElementDescriptor("outputwindow", InterfaceElementDescriptor.InterfaceElementDescriptorType.Main);
            outputwindowMain.CoordinateAttributes["pos"] = new Point(0, 0);
            outputwindowMain.DimensionAttributes["size"] = new Size(640, 480);
            outputwindowMain.BoolAttributes["is-pane"] = true;
            InterfaceElementDescriptor output = new InterfaceElementDescriptor("output", InterfaceElementDescriptor.InterfaceElementDescriptorType.Output);
            output.CoordinateAttributes["pos"] = new Point(0, 0);
            output.DimensionAttributes["size"] = new Size(640, 480);
            output.CoordinateAttributes["anchor1"] = new Point(0, 0);
            output.CoordinateAttributes["anchor2"] = new Point(100, 100);
            output.BoolAttributes["is-default"] = true;
            InterfaceWindowDescriptor outputwindow = new InterfaceWindowDescriptor("outputwindow", new List<InterfaceElementDescriptor>() { outputwindowMain, output });

            InterfaceElementDescriptor statwindowMain = new InterfaceElementDescriptor("statwindow", InterfaceElementDescriptor.InterfaceElementDescriptorType.Main);
            statwindowMain.CoordinateAttributes["pos"] = new Point(0, 0);
            statwindowMain.DimensionAttributes["size"] = new Size(640, 480);
            statwindowMain.BoolAttributes["is-pane"] = true;
            InterfaceElementDescriptor stat = new InterfaceElementDescriptor("output", InterfaceElementDescriptor.InterfaceElementDescriptorType.Info);
            stat.CoordinateAttributes["pos"] = new Point(0, 0);
            stat.DimensionAttributes["size"] = new Size(640, 480);
            stat.CoordinateAttributes["anchor1"] = new Point(0, 0);
            stat.CoordinateAttributes["anchor2"] = new Point(100, 100);
            stat.BoolAttributes["is-default"] = true;
            InterfaceWindowDescriptor statwindow = new InterfaceWindowDescriptor("statwindow", new List<InterfaceElementDescriptor>() { statwindowMain, stat });

            InterfaceElementDescriptor mainwindowMain = new InterfaceElementDescriptor("mainwindow", InterfaceElementDescriptor.InterfaceElementDescriptorType.Main);
            mainwindowMain.DimensionAttributes["size"] = new Size(640, 440);
            mainwindowMain.BoolAttributes["is-default"] = true;
            InterfaceElementDescriptor split = new InterfaceElementDescriptor("split", InterfaceElementDescriptor.InterfaceElementDescriptorType.Child);
            split.CoordinateAttributes["pos"] = new Point(3, 0);
            split.DimensionAttributes["size"] = new Size(634, 417);
            split.CoordinateAttributes["anchor1"] = new Point(0, 0);
            split.CoordinateAttributes["anchor2"] = new Point(100, 100);
            split.StringAttributes["left"] = "mapwindow";
            split.StringAttributes["right"] = "infowindow";
            split.BoolAttributes["is-vert"] = true;
            InterfaceWindowDescriptor mainwindow = new InterfaceWindowDescriptor("mainwindow", new List<InterfaceElementDescriptor>() { mainwindowMain, split });
            
            return new InterfaceDescriptor(new List<InterfaceWindowDescriptor>() { mainwindow, mapwindow, infowindow, outputwindow, statwindow });
        }

        private static void RegisterNativeProcs() {
            DreamObjectTree.RegisterNativeProc("abs", new DreamProc(DreamProcNativeRoot.NativeProc_abs, new List<string>() { "A" }));
            DreamObjectTree.RegisterNativeProc("block", new DreamProc(DreamProcNativeRoot.NativeProc_block, new List<string>() { "Start", "End" }));
            DreamObjectTree.RegisterNativeProc("browse", new DreamProc(DreamProcNativeRoot.NativeProc_browse, new List<string>() { "Body", "Options" }));
            DreamObjectTree.RegisterNativeProc("ckey", new DreamProc(DreamProcNativeRoot.NativeProc_ckey, new List<string>() { "Key" }));
            DreamObjectTree.RegisterNativeProc("copytext", new DreamProc(DreamProcNativeRoot.NativeProc_copytext, new List<string>() { "T", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("CRASH", new DreamProc(DreamProcNativeRoot.NativeProc_CRASH, new List<string>() { "msg" }));
            DreamObjectTree.RegisterNativeProc("fexists", new DreamProc(DreamProcNativeRoot.NativeProc_fexists, new List<string>() { "File" }));
            DreamObjectTree.RegisterNativeProc("file2text", new DreamProc(DreamProcNativeRoot.NativeProc_file2text, new List<string>() { "File" }));
            DreamObjectTree.RegisterNativeProc("findtext", new DreamProc(DreamProcNativeRoot.NativeProc_findtext, new List<string>() { "Haystack", "Needle", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("findlasttext", new DreamProc(DreamProcNativeRoot.NativeProc_findlasttext, new List<string>() { "Haystack", "Needle", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("get_dist", new DreamProc(DreamProcNativeRoot.NativeProc_get_dist, new List<string>() { "Loc1", "Loc2" }));
            DreamObjectTree.RegisterNativeProc("image", new DreamProc(DreamProcNativeRoot.NativeProc_image, new List<string>() { "icon", "loc", "icon_state", "layer", "dir" }));
            DreamObjectTree.RegisterNativeProc("isloc", new DreamProc(DreamProcNativeRoot.NativeProc_isloc, new List<string>() { "Loc1" }));
            DreamObjectTree.RegisterNativeProc("ismob", new DreamProc(DreamProcNativeRoot.NativeProc_ismob, new List<string>() { "Loc1" }));
            DreamObjectTree.RegisterNativeProc("isnull", new DreamProc(DreamProcNativeRoot.NativeProc_isnull, new List<string>() { "Val" }));
            DreamObjectTree.RegisterNativeProc("isnum", new DreamProc(DreamProcNativeRoot.NativeProc_isnum, new List<string>() { "Val" }));
            DreamObjectTree.RegisterNativeProc("ispath", new DreamProc(DreamProcNativeRoot.NativeProc_ispath, new List<string>() { "Val", "Type" }));
            DreamObjectTree.RegisterNativeProc("istext", new DreamProc(DreamProcNativeRoot.NativeProc_istext, new List<string>() { "Val" }));
            DreamObjectTree.RegisterNativeProc("isturf", new DreamProc(DreamProcNativeRoot.NativeProc_isturf, new List<string>() { "Loc1" }));
            DreamObjectTree.RegisterNativeProc("istype", new DreamProc(DreamProcNativeRoot.NativeProc_istype, new List<string>() { "Val", "Type" }));
            DreamObjectTree.RegisterNativeProc("json_decode", new DreamProc(DreamProcNativeRoot.NativeProc_json_decode, new List<string>() { "JSON" }));
            DreamObjectTree.RegisterNativeProc("length", new DreamProc(DreamProcNativeRoot.NativeProc_length, new List<string>() { "E" }));
            DreamObjectTree.RegisterNativeProc("list", new DreamProc(DreamProcNativeRoot.NativeProc_list, new List<string>()));
            DreamObjectTree.RegisterNativeProc("locate", new DreamProc(DreamProcNativeRoot.NativeProc_locate, new List<string>() { "X", "Y", "Z" }));
            DreamObjectTree.RegisterNativeProc("lowertext", new DreamProc(DreamProcNativeRoot.NativeProc_lowertext, new List<string>() { "T" }));
            DreamObjectTree.RegisterNativeProc("max", new DreamProc(DreamProcNativeRoot.NativeProc_max, new List<string>() { "A" }));
            DreamObjectTree.RegisterNativeProc("min", new DreamProc(DreamProcNativeRoot.NativeProc_min, new List<string>() { "A" }));
            DreamObjectTree.RegisterNativeProc("orange", new DreamProc(DreamProcNativeRoot.NativeProc_orange, new List<string>() { "Dist", "Center" }));
            DreamObjectTree.RegisterNativeProc("pick", new DreamProc(DreamProcNativeRoot.NativeProc_pick, new List<string>() { "Val1" }));
            DreamObjectTree.RegisterNativeProc("prob", new DreamProc(DreamProcNativeRoot.NativeProc_prob, new List<string>() { "P" }));
            DreamObjectTree.RegisterNativeProc("rand", new DreamProc(DreamProcNativeRoot.NativeProc_pick, new List<string>()));
            DreamObjectTree.RegisterNativeProc("replacetext", new DreamProc(DreamProcNativeRoot.NativeProc_replacetext, new List<string>() { "Haystack", "Needle", "Replacement", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("round", new DreamProc(DreamProcNativeRoot.NativeProc_round, new List<string>() { "A", "B" }));
            DreamObjectTree.RegisterNativeProc("sleep", new DreamProc(DreamProcNativeRoot.NativeProc_sleep, new List<string>() { "Delay" }));
            DreamObjectTree.RegisterNativeProc("sound", new DreamProc(DreamProcNativeRoot.NativeProc_sound, new List<string>() { "file", "repeat", "wait", "channel", "volume" }, new Dictionary<string, DreamValue>() { { "repeat", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("splittext", new DreamProc(DreamProcNativeRoot.NativeProc_splittext, new List<string>() { "Text", "Delimiter", "Start", "End", "include_delimiters" }));
            DreamObjectTree.RegisterNativeProc("text2ascii", new DreamProc(DreamProcNativeRoot.NativeProc_text2ascii, new List<string>() { "T", "pos" }, new Dictionary<string, DreamValue>() { { "pos", new DreamValue(1) } }));
            DreamObjectTree.RegisterNativeProc("text2num", new DreamProc(DreamProcNativeRoot.NativeProc_text2num, new List<string>() { "T", "radix" }, new Dictionary<string, DreamValue>() { { "radix", new DreamValue(10) } }));
            DreamObjectTree.RegisterNativeProc("text2path", new DreamProc(DreamProcNativeRoot.NativeProc_text2path, new List<string>() { "T" }));
            DreamObjectTree.RegisterNativeProc("typesof", new DreamProc(DreamProcNativeRoot.NativeProc_typesof, new List<string>() { "Item1" }));
            DreamObjectTree.RegisterNativeProc("uppertext", new DreamProc(DreamProcNativeRoot.NativeProc_uppertext, new List<string>() { "T" }));
            DreamObjectTree.RegisterNativeProc("view", new DreamProc(DreamProcNativeRoot.NativeProc_view, new List<string>() { "Dist", "Center" }, new Dictionary<string, DreamValue>() { { "Dist", new DreamValue(4) } }));
            DreamObjectTree.RegisterNativeProc("walk", new DreamProc(DreamProcNativeRoot.NativeProc_walk, new List<string>() { "Ref", "Dir", "Lag", "Speed" }, new Dictionary<string, DreamValue>() { { "Lag", new DreamValue(0) }, { "Speed", new DreamValue(0) } }));

            DreamObjectTree.RegisterNativeProc("list_Add", new DreamProc(DreamProcNativeList.NativeProc_Add, new List<string>() { "Item1" }));
            DreamObjectTree.RegisterNativeProc("list_Copy", new DreamProc(DreamProcNativeList.NativeProc_Copy, new List<string>() { "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("list_Cut", new DreamProc(DreamProcNativeList.NativeProc_Cut, new List<string>() { "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("list_Find", new DreamProc(DreamProcNativeList.NativeProc_Find, new List<string>() { "Elem", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("list_Join", new DreamProc(DreamProcNativeList.NativeProc_Join, new List<string>() { "Glue", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("list_Remove", new DreamProc(DreamProcNativeList.NativeProc_Remove, new List<string>() { "Item1" }));
        }

        private static void CreateAtomBases() {
            DreamObjectTree.DreamObjectTreeEntry atomTreeEntry = DreamObjectTree.GetTreeEntryFromPath(DreamPath.Atom);
            List<DreamObjectTree.DreamObjectTreeEntry> atomDescendants = atomTreeEntry.GetAllDescendants(true, true);
            UInt16 atomBaseIDCounter = 0;

            ATOMBase.AtomBases = new Dictionary<UInt16, ATOMBase>();
            foreach (DreamObjectTree.DreamObjectTreeEntry treeEntry in atomDescendants) {
                DreamObjectDefinition objectDefinition = treeEntry.ObjectDefinition;
                IconVisualProperties visualProperties = new IconVisualProperties();
                DreamValue iconValue = objectDefinition.Variables["icon"];
                DreamValue iconStateValue = objectDefinition.Variables["icon_state"];

                if (iconValue.Type == DreamValue.DreamValueType.DreamResource) {
                    visualProperties.Icon = ((DreamResource)iconValue.Value).ResourcePath;
                }
                
                if (iconStateValue.Type == DreamValue.DreamValueType.String) {
                    visualProperties.IconState = (string)iconStateValue.Value;
                }

                ATOMType atomType = ATOMType.Atom;
                if (objectDefinition.IsSubtypeOf(DreamPath.Area)) atomType = ATOMType.Area;
                else if (objectDefinition.IsSubtypeOf(DreamPath.Turf)) atomType = ATOMType.Turf;
                else if (objectDefinition.IsSubtypeOf(DreamPath.Mob)) atomType = ATOMType.Movable;
                else if (objectDefinition.IsSubtypeOf(DreamPath.Obj)) atomType = ATOMType.Movable;

                ATOMBase atomBase = new ATOMBase(atomBaseIDCounter++, atomType, visualProperties);
                ATOMBase.AtomBases.Add(atomBase.ID, atomBase);
                AtomBaseIDs.Add(objectDefinition, atomBase.ID);
            }
        }

        private static void OnDeltaStateFinalized(DreamDeltaState deltaState) {
            foreach (DreamConnection dreamConnection in DreamServer.DreamConnections) {
                dreamConnection.SendPacket(new PacketDeltaGameState(deltaState));
            }
        }

        private static void OnDreamConnectionRequest(DreamConnection connection) {
            Console.WriteLine("Connection request from '" + connection.CKey + "'");

            connection.ClientDreamObject = DreamObjectTree.CreateObject(DreamPath.Client, new DreamProcArguments(new List<DreamValue>() { new DreamValue((DreamObject)null) }));
            ClientToConnection[connection.ClientDreamObject] = connection;
            connection.SendPacket(new PacketInterfaceData(clientInterface));
            DreamValue clientMob = connection.ClientDreamObject.CallProc("New");

            if (clientMob.Value != null) {
                connection.SendPacket(new PacketConnectionResult(true, ""));
                connection.SendPacket(new PacketATOMTypes(ATOMBase.AtomBases));
                connection.SendPacket(new PacketFullGameState(DreamStateManager.CreateLatestFullState()));
            } else {
                connection.SendPacket(new PacketConnectionResult(false, "The connection was disallowed"));
            }
        }
    }
}
