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
                    if (pClickAtom.ModifierAlt) paramsBuilder.Add("alt", "1");

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
            SetNativeProcs();
            CreateAtomBases();
            
            WorldInstance = DreamObjectTree.CreateObject(DreamPath.World);
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

        private static void SetNativeProcs() {
            DreamObjectDefinition root = DreamObjectTree.GetObjectDefinitionFromPath(DreamPath.Root);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_abs);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_animate);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_ascii2text);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_ckey);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_copytext);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_CRASH);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_fcopy);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_fcopy_rsc);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_fdel);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_fexists);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_file);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_file2text);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_findtext);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_findtextEx);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_findlasttext);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_get_dist);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_html_decode);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_html_encode);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_image);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_isarea);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_isloc);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_ismob);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_isnull);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_isnum);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_ispath);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_istext);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_isturf);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_istype);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_json_decode);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_json_encode);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_length);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_locate);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_log);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_lowertext);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_max);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_min);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_num2text);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_orange);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_params2list);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_pick);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_prob);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_rand);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_replacetext);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_round);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_sleep);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_sorttext);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_sorttextEx);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_sound);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_splittext);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_text);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_text2ascii);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_text2file);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_text2num);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_text2path);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_time2text);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_typesof);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_uppertext);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_url_encode);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_view);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_viewers);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_walk);
            root.SetNativeProc(DreamProcNativeRoot.NativeProc_walk_to);

            DreamObjectDefinition list = DreamObjectTree.GetObjectDefinitionFromPath(DreamPath.List);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Add);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Copy);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Cut);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Find);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Insert);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Join);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Remove);
            list.SetNativeProc(DreamProcNativeList.NativeProc_Swap);
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
                DreamValue colorValue = objectDefinition.Variables["color"];
                DreamValue iconLayer = objectDefinition.Variables["layer"];

                if (iconValue.Type == DreamValue.DreamValueType.DreamResource) {
                    visualProperties.Icon = ((DreamResource)iconValue.Value).ResourcePath;
                }
                
                if (iconStateValue.Type == DreamValue.DreamValueType.String) {
                    visualProperties.IconState = (string)iconStateValue.Value;
                }

                if (colorValue.Type == DreamValue.DreamValueType.String) {
                    visualProperties.SetColor((string)colorValue.Value);
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
