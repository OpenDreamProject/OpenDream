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
using OpenDreamShared.Json;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDreamServer {
    class Program {
        public static DreamCompiledJson CompiledJson = null;
        public static DreamResourceManager DreamResourceManager = null;
        public static DreamStateManager DreamStateManager = new DreamStateManager();
        public static DreamObjectTree DreamObjectTree = new DreamObjectTree();
        public static DreamMap DreamMap = null;
        public static DreamServer DreamServer = new DreamServer(25566);
        public static DreamObject WorldInstance = null;
        public static Dictionary<DreamObject, DreamConnection> ClientToConnection = new();
        public static List<CountdownEvent> TickEvents = new();
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

            RegisterPacketCallbacks();

            DreamResource compiledJsonResource = DreamResourceManager.LoadResource(objectTreeFile);
            CompiledJson = JsonSerializer.Deserialize<DreamCompiledJson>(compiledJsonResource.ReadAsString());

            DreamObjectTree.LoadFromJson(CompiledJson.RootObject);
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
            DreamProcNative.SetupNativeProcs();
            
            TickStartTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();

            WorldInstance = DreamObjectTree.CreateObject(DreamPath.World);
            DreamObjectTree.GetObjectDefinitionFromPath(DreamPath.Root).GlobalVariables["world"].Value = new DreamValue(WorldInstance);

            DreamProc globalInitProc = new DreamProc(CompiledJson.GlobalInitProc.Bytecode);
            globalInitProc.Run(WorldInstance, new DreamProcArguments(new(), new()));

            DreamMap = new DreamMap();
            DreamMap.LoadMap(DreamResourceManager.LoadResource(mapFile));

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

        private static void RegisterPacketCallbacks() {
            DreamServer.RegisterPacketCallback<PacketRequestResource>(PacketID.RequestResource, DreamResourceManager.HandleRequestResourcePacket);
            DreamServer.RegisterPacketCallback(PacketID.KeyboardInput, (DreamConnection connection, PacketKeyboardInput pKeyboardInput) => connection.HandlePacketKeyboardInput(pKeyboardInput));;
            DreamServer.RegisterPacketCallback(PacketID.ClickAtom, (DreamConnection connection, PacketClickAtom pClickAtom) => connection.HandlePacketClickAtom(pClickAtom)); ;
            DreamServer.RegisterPacketCallback(PacketID.Topic, (DreamConnection connection, PacketTopic pTopic) => connection.HandlePacketTopic(pTopic)); ;
            DreamServer.RegisterPacketCallback(PacketID.PromptResponse, (DreamConnection connection, PacketPromptResponse pPromptResponse) => connection.HandlePacketPromptResponse(pPromptResponse));
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
            connection.SendPacket(new PacketFullGameState(DreamStateManager.CreateLatestFullState()));

            Task.Run(() => {
                DreamValue clientMob = connection.ClientDreamObject.CallProc("New");
                if (clientMob.Value != null) {
                    connection.SendPacket(new PacketConnectionResult(true, ""));
                } else {
                    connection.SendPacket(new PacketConnectionResult(false, "The connection was disallowed"));
                }
            });
        }
    }
}
