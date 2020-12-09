using OpenDreamServer.Dream;
using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Dream.Procs.Native;
using OpenDreamServer.Net;
using OpenDreamServer.Resources;
using OpenDreamShared.Compiler.DMF;
using OpenDreamShared.Dream;
using OpenDreamShared.Interface;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

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
        public static List<CountdownEvent> TickEvents = new List<CountdownEvent>();
        public static int TickCount = 0;
        public static long TickStartTime = 0;

        private static InterfaceDescriptor _clientInterface = null;

        static void Main(string[] args) {
            if (args.Length < 4) {
                Console.WriteLine("Four arguments are required:");
                Console.WriteLine("\tResource Location");
                Console.WriteLine("\t\tPath to the folder holding all the game's assets");
                Console.WriteLine("\tObject Tree Location");
                Console.WriteLine("\t\tPath to the JSON file holding a compiled form of the game's code, relative to the resource location");
                Console.WriteLine("\tMap File Location");
                Console.WriteLine("\t\tPath to the map's DMM file, relative to the resource location");
                Console.WriteLine("\tInterface File Location");
                Console.WriteLine("\t\tPath to the interface's DMF file, relative to the resource location");

                return;
            }

            string resourceLocation = args[0];
            string objectTreeFile = args[1];
            string mapFile = args[2];
            string interfaceFile = args[3];

            DreamResourceManager = new DreamResourceManager(resourceLocation);
            DreamResource interfaceResource = DreamResourceManager.LoadResource(interfaceFile);
            DMFLexer dmfLexer = new DMFLexer(interfaceResource.ReadAsString());
            DMFParser dmfParser = new DMFParser(dmfLexer);
            _clientInterface = dmfParser.Interface();

            DreamStateManager.DeltaStateFinalized += OnDeltaStateFinalized;
            DreamServer.DreamConnectionRequest += OnDreamConnectionRequest;

            DreamServer.RegisterPacketCallback<PacketRequestResource>(PacketID.RequestResource, DreamResourceManager.HandleRequestResourcePacket);
            DreamServer.RegisterPacketCallback<PacketKeyboardInput>(PacketID.KeyboardInput, (DreamConnection connection, PacketKeyboardInput pKeyboardInput) => {
                foreach (int key in pKeyboardInput.KeysDown) {
                    if (!connection.PressedKeys.Contains(key)) connection.PressedKeys.Add(key);
                }

                foreach (int key in pKeyboardInput.KeysUp) {
                    connection.PressedKeys.Remove(key);
                }
            });
            DreamServer.RegisterPacketCallback<PacketClickAtom>(PacketID.ClickAtom, (DreamConnection connection, PacketClickAtom pClickAtom) => {
                if (DreamMetaObjectAtom.AtomIDToAtom.TryGetValue(pClickAtom.AtomID, out DreamObject atom)) {
                    NameValueCollection paramsBuilder = HttpUtility.ParseQueryString(String.Empty);
                    paramsBuilder.Add("icon-x", pClickAtom.IconX.ToString());
                    paramsBuilder.Add("icon-y", pClickAtom.IconY.ToString());
                    if (pClickAtom.ModifierShift) paramsBuilder.Add("shift", "1");
                    if (pClickAtom.ModifierCtrl) paramsBuilder.Add("ctrl", "1");

                    DreamProcArguments clickArguments = new DreamProcArguments(new() {
                        new DreamValue(atom),
                        new DreamValue((DreamObject)null),
                        new DreamValue((DreamObject)null),
                        new DreamValue(paramsBuilder.ToString())
                    });
                    
                    Task.Run(() => connection.ClientDreamObject?.CallProc("Click", clickArguments, connection.MobDreamObject));
                }
            });
            DreamServer.RegisterPacketCallback<PacketTopic>(PacketID.Topic, (DreamConnection connection, PacketTopic pTopic) => {
                DreamObject hrefListObject = DreamProcNativeRoot.params2list(pTopic.Query);
                DreamList hrefList = DreamMetaObjectList.DreamLists[hrefListObject];
                DreamValue srcRefValue = hrefList.GetValue(new DreamValue("src"));
                DreamObject src = null;

                if (srcRefValue.Value != null) {
                    int srcRef = int.Parse(srcRefValue.GetValueAsString());

                    src = DreamObject.GetFromReferenceID(srcRef);
                }

                DreamProcArguments topicArguments = new DreamProcArguments(new() {
                    new DreamValue(pTopic.Query),
                    new DreamValue(hrefListObject),
                    new DreamValue(src)
                });

                Task.Run(() => connection.ClientDreamObject?.CallProc("Topic", topicArguments, connection.MobDreamObject));
            });

            RegisterNativeProcs();
            DreamObjectTree.LoadFromJson(DreamResourceManager.LoadResource(objectTreeFile).ReadAsString());
            DreamObjectTree.SetMetaObject(DreamPath.Root, new DreamMetaObjectRoot());
            DreamObjectTree.SetMetaObject(DreamPath.List, new DreamMetaObjectList());
            DreamObjectTree.SetMetaObject(DreamPath.Sound, new DreamMetaObjectSound());
            DreamObjectTree.SetMetaObject(DreamPath.Image, new DreamMetaObjectImage());
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

            TickStartTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
            Task.Run(() => WorldInstance.CallProc("New"));
            DreamServer.Start();
            while (true) {
                TickStartTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                for (int i = 0; i < TickEvents.Count; i++) {
                    CountdownEvent tickEvent = TickEvents[i];

                    tickEvent.Signal();
                    if (tickEvent.CurrentCount <= 0) {
                        TickEvents.RemoveAt(i);
                        i--;
                    }
                }

                foreach (DreamConnection connection in DreamServer.DreamConnections) {
                    if (connection.PressedKeys.Contains(38)) {
                        Task.Run(() => { connection.ClientDreamObject?.CallProc("North"); });
                    } else if (connection.PressedKeys.Contains(39)) {
                        Task.Run(() => { connection.ClientDreamObject?.CallProc("East"); });
                    } else if (connection.PressedKeys.Contains(40)) {
                        Task.Run(() => { connection.ClientDreamObject?.CallProc("South"); });
                    } else if (connection.PressedKeys.Contains(37)) {
                        Task.Run(() => { connection.ClientDreamObject?.CallProc("West"); });
                    }
                }

                DreamStateManager.FinalizeCurrentDeltaState();
                DreamServer.Process();

                TickCount++;
                Thread.Sleep((int)(100 * WorldInstance.GetVariable("tick_lag").GetValueAsNumber()));
            }
        }

        private static void RegisterNativeProcs() {
            DreamObjectTree.RegisterNativeProc("abs", new DreamProc(DreamProcNativeRoot.NativeProc_abs, new List<string>() { "A" }));
            DreamObjectTree.RegisterNativeProc("animate", new DreamProc(DreamProcNativeRoot.NativeProc_animate, new List<string>() { "Object", "time", "loop", "easing", "flags" }));
            DreamObjectTree.RegisterNativeProc("ascii2text", new DreamProc(DreamProcNativeRoot.NativeProc_ascii2text, new List<string>() { "N" }));
            DreamObjectTree.RegisterNativeProc("ckey", new DreamProc(DreamProcNativeRoot.NativeProc_ckey, new List<string>() { "Key" }));
            DreamObjectTree.RegisterNativeProc("copytext", new DreamProc(DreamProcNativeRoot.NativeProc_copytext, new List<string>() { "T", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("CRASH", new DreamProc(DreamProcNativeRoot.NativeProc_CRASH, new List<string>() { "msg" }));
            DreamObjectTree.RegisterNativeProc("fcopy", new DreamProc(DreamProcNativeRoot.NativeProc_fcopy, new List<string>() { "Src", "Dst" }));
            DreamObjectTree.RegisterNativeProc("fcopy_rsc", new DreamProc(DreamProcNativeRoot.NativeProc_fcopy_rsc, new List<string>() { "File" }));
            DreamObjectTree.RegisterNativeProc("fdel", new DreamProc(DreamProcNativeRoot.NativeProc_fdel, new List<string>() { "File" }));
            DreamObjectTree.RegisterNativeProc("fexists", new DreamProc(DreamProcNativeRoot.NativeProc_fexists, new List<string>() { "File" }));
            DreamObjectTree.RegisterNativeProc("file", new DreamProc(DreamProcNativeRoot.NativeProc_file, new List<string>() { "Path" }));
            DreamObjectTree.RegisterNativeProc("file2text", new DreamProc(DreamProcNativeRoot.NativeProc_file2text, new List<string>() { "File" }));
            DreamObjectTree.RegisterNativeProc("findtext", new DreamProc(DreamProcNativeRoot.NativeProc_findtext, new List<string>() { "Haystack", "Needle", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("findtextEx", new DreamProc(DreamProcNativeRoot.NativeProc_findtextEx, new List<string>() { "Haystack", "Needle", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("findlasttext", new DreamProc(DreamProcNativeRoot.NativeProc_findlasttext, new List<string>() { "Haystack", "Needle", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("get_dir", new DreamProc(DreamProcNativeRoot.NativeProc_get_dir, new List<string>() { "Loc1", "Loc2" }));
            DreamObjectTree.RegisterNativeProc("get_dist", new DreamProc(DreamProcNativeRoot.NativeProc_get_dist, new List<string>() { "Loc1", "Loc2" }));
            DreamObjectTree.RegisterNativeProc("html_decode", new DreamProc(DreamProcNativeRoot.NativeProc_html_decode, new List<string>() { "HtmlText" }));
            DreamObjectTree.RegisterNativeProc("html_encode", new DreamProc(DreamProcNativeRoot.NativeProc_html_encode, new List<string>() { "PlainText" }));
            DreamObjectTree.RegisterNativeProc("image", new DreamProc(DreamProcNativeRoot.NativeProc_image, new List<string>() { "icon", "loc", "icon_state", "layer", "dir" }));
            DreamObjectTree.RegisterNativeProc("isarea", new DreamProc(DreamProcNativeRoot.NativeProc_isarea, new List<string>() { "Loc1" }));
            DreamObjectTree.RegisterNativeProc("isloc", new DreamProc(DreamProcNativeRoot.NativeProc_isloc, new List<string>() { "Loc1" }));
            DreamObjectTree.RegisterNativeProc("ismob", new DreamProc(DreamProcNativeRoot.NativeProc_ismob, new List<string>() { "Loc1" }));
            DreamObjectTree.RegisterNativeProc("isnull", new DreamProc(DreamProcNativeRoot.NativeProc_isnull, new List<string>() { "Val" }));
            DreamObjectTree.RegisterNativeProc("isnum", new DreamProc(DreamProcNativeRoot.NativeProc_isnum, new List<string>() { "Val" }));
            DreamObjectTree.RegisterNativeProc("ispath", new DreamProc(DreamProcNativeRoot.NativeProc_ispath, new List<string>() { "Val", "Type" }));
            DreamObjectTree.RegisterNativeProc("istext", new DreamProc(DreamProcNativeRoot.NativeProc_istext, new List<string>() { "Val" }));
            DreamObjectTree.RegisterNativeProc("isturf", new DreamProc(DreamProcNativeRoot.NativeProc_isturf, new List<string>() { "Loc1" }));
            DreamObjectTree.RegisterNativeProc("istype", new DreamProc(DreamProcNativeRoot.NativeProc_istype, new List<string>() { "Val", "Type" }));
            DreamObjectTree.RegisterNativeProc("json_decode", new DreamProc(DreamProcNativeRoot.NativeProc_json_decode, new List<string>() { "JSON" }));
            DreamObjectTree.RegisterNativeProc("json_encode", new DreamProc(DreamProcNativeRoot.NativeProc_json_encode, new List<string>() { "Value" }));
            DreamObjectTree.RegisterNativeProc("length", new DreamProc(DreamProcNativeRoot.NativeProc_length, new List<string>() { "E" }));
            DreamObjectTree.RegisterNativeProc("locate", new DreamProc(DreamProcNativeRoot.NativeProc_locate, new List<string>() { "X", "Y", "Z" }));
            DreamObjectTree.RegisterNativeProc("lowertext", new DreamProc(DreamProcNativeRoot.NativeProc_lowertext, new List<string>() { "T" }));
            DreamObjectTree.RegisterNativeProc("max", new DreamProc(DreamProcNativeRoot.NativeProc_max, new List<string>() { "A" }));
            DreamObjectTree.RegisterNativeProc("min", new DreamProc(DreamProcNativeRoot.NativeProc_min, new List<string>() { "A" }));
            DreamObjectTree.RegisterNativeProc("num2text", new DreamProc(DreamProcNativeRoot.NativeProc_num2text, new List<string>() { "N", "Digits", "Radix" }));
            DreamObjectTree.RegisterNativeProc("orange", new DreamProc(DreamProcNativeRoot.NativeProc_orange, new List<string>() { "Dist", "Center" }));
            DreamObjectTree.RegisterNativeProc("params2list", new DreamProc(DreamProcNativeRoot.NativeProc_params2list, new List<string>() { "Params" }));
            DreamObjectTree.RegisterNativeProc("pick", new DreamProc(DreamProcNativeRoot.NativeProc_pick, new List<string>() { "Val1" }));
            DreamObjectTree.RegisterNativeProc("prob", new DreamProc(DreamProcNativeRoot.NativeProc_prob, new List<string>() { "P" }));
            DreamObjectTree.RegisterNativeProc("rand", new DreamProc(DreamProcNativeRoot.NativeProc_rand, new List<string>()));
            DreamObjectTree.RegisterNativeProc("replacetext", new DreamProc(DreamProcNativeRoot.NativeProc_replacetext, new List<string>() { "Haystack", "Needle", "Replacement", "Start", "End" }, new Dictionary<string, DreamValue>() { { "Start", new DreamValue(1) }, { "End", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("round", new DreamProc(DreamProcNativeRoot.NativeProc_round, new List<string>() { "A", "B" }));
            DreamObjectTree.RegisterNativeProc("sleep", new DreamProc(DreamProcNativeRoot.NativeProc_sleep, new List<string>() { "Delay" }));
            DreamObjectTree.RegisterNativeProc("sound", new DreamProc(DreamProcNativeRoot.NativeProc_sound, new List<string>() { "file", "repeat", "wait", "channel", "volume" }, new Dictionary<string, DreamValue>() { { "repeat", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("splittext", new DreamProc(DreamProcNativeRoot.NativeProc_splittext, new List<string>() { "Text", "Delimiter", "Start", "End", "include_delimiters" }));
            DreamObjectTree.RegisterNativeProc("text", new DreamProc(DreamProcNativeRoot.NativeProc_text, new List<string>() { "FormatText" }));
            DreamObjectTree.RegisterNativeProc("text2ascii", new DreamProc(DreamProcNativeRoot.NativeProc_text2ascii, new List<string>() { "T", "pos" }, new Dictionary<string, DreamValue>() { { "pos", new DreamValue(1) } }));
            DreamObjectTree.RegisterNativeProc("text2file", new DreamProc(DreamProcNativeRoot.NativeProc_text2file, new List<string>() { "Text", "File" }));
            DreamObjectTree.RegisterNativeProc("text2num", new DreamProc(DreamProcNativeRoot.NativeProc_text2num, new List<string>() { "T", "radix" }, new Dictionary<string, DreamValue>() { { "radix", new DreamValue(10) } }));
            DreamObjectTree.RegisterNativeProc("text2path", new DreamProc(DreamProcNativeRoot.NativeProc_text2path, new List<string>() { "T" }));
            DreamObjectTree.RegisterNativeProc("time2text", new DreamProc(DreamProcNativeRoot.NativeProc_time2text, new List<string>() { "timestamp", "format" }));
            DreamObjectTree.RegisterNativeProc("typesof", new DreamProc(DreamProcNativeRoot.NativeProc_typesof, new List<string>() { "Item1" }));
            DreamObjectTree.RegisterNativeProc("uppertext", new DreamProc(DreamProcNativeRoot.NativeProc_uppertext, new List<string>() { "T" }));
            DreamObjectTree.RegisterNativeProc("url_encode", new DreamProc(DreamProcNativeRoot.NativeProc_url_encode, new List<string>() { "PlainText", "format" }, new Dictionary<string, DreamValue>() { { "format", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("view", new DreamProc(DreamProcNativeRoot.NativeProc_view, new List<string>() { "Dist", "Center" }, new Dictionary<string, DreamValue>() { { "Dist", new DreamValue(4) } }));
            DreamObjectTree.RegisterNativeProc("viewers", new DreamProc(DreamProcNativeRoot.NativeProc_viewers, new List<string>() { "Depth", "Center" }));
            DreamObjectTree.RegisterNativeProc("walk", new DreamProc(DreamProcNativeRoot.NativeProc_walk, new List<string>() { "Ref", "Dir", "Lag", "Speed" }, new Dictionary<string, DreamValue>() { { "Lag", new DreamValue(0) }, { "Speed", new DreamValue(0) } }));
            DreamObjectTree.RegisterNativeProc("walk_to", new DreamProc(DreamProcNativeRoot.NativeProc_walk_to, new List<string>() { "Ref", "Trg", "Min", "Lag", "Speed" }, new Dictionary<string, DreamValue>() { { "Min", new DreamValue(0) }, { "Lag", new DreamValue(0) }, { "Speed", new DreamValue(0) } }));

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
                DreamValue iconLayer = objectDefinition.Variables["layer"];

                if (iconValue.Type == DreamValue.DreamValueType.DreamResource) {
                    visualProperties.Icon = ((DreamResource)iconValue.Value).ResourcePath;
                }
                
                if (iconStateValue.Type == DreamValue.DreamValueType.String) {
                    visualProperties.IconState = (string)iconStateValue.Value;
                }

                visualProperties.Layer = (float)iconLayer.GetValueAsNumber();

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
                dreamConnection.SendPacket(new PacketDeltaGameState(deltaState, dreamConnection.CKey));
            }
        }

        private static void OnDreamConnectionRequest(DreamConnection connection) {
            Console.WriteLine("Connection request from '" + connection.CKey + "'");
            DreamStateManager.AddClient(connection.CKey);

            connection.ClientDreamObject = DreamObjectTree.CreateObject(DreamPath.Client, new DreamProcArguments(new List<DreamValue>() { new DreamValue((DreamObject)null) }));
            ClientToConnection[connection.ClientDreamObject] = connection;
            connection.SendPacket(new PacketInterfaceData(_clientInterface));

            Task.Run(() => {
                connection.SendPacket(new PacketATOMTypes(ATOMBase.AtomBases));

                DreamValue clientMob = connection.ClientDreamObject.CallProc("New");
                if (clientMob.Value != null) {
                    connection.SendPacket(new PacketConnectionResult(true, ""));
                    connection.SendPacket(new PacketFullGameState(DreamStateManager.CreateLatestFullState()));
                } else {
                    connection.SendPacket(new PacketConnectionResult(false, "The connection was disallowed"));
                }
            });
        }
    }
}
