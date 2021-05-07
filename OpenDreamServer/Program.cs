using OpenDreamServer.Dream;
using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Dream.Procs.Native;
using OpenDreamServer.Net;
using OpenDreamServer.Resources;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DMF;
using OpenDreamShared.Dream;
using OpenDreamShared.Interface;
using OpenDreamShared.Json;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.IO;
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
            if (args.Length < 1 || Path.GetExtension(args[0]) != ".json") {
                Console.WriteLine("You must compile your game using DMCompiler, and supply its output as an argument");

                return;
            }

            string compiledDreamFilepath = args[0];
            string resourcePath = Path.GetDirectoryName(compiledDreamFilepath);

            DreamResourceManager = new DreamResourceManager(resourcePath);

            if (!LoadCompiledDreamJson(compiledDreamFilepath)) return;
            if (!LoadInterface(CompiledJson.Interface)) return;

            DreamStateManager.DeltaStateFinalized += OnDeltaStateFinalized;
            DreamServer.DreamConnectionRequest += OnDreamConnectionRequest;

            RegisterPacketCallbacks();

            DreamObjectTree.LoadFromJson(CompiledJson.RootObject);
            DreamObjectTree.SetMetaObject(DreamPath.Root, new DreamMetaObjectRoot());
            DreamObjectTree.SetMetaObject(DreamPath.List, new DreamMetaObjectList());
            DreamObjectTree.SetMetaObject(DreamPath.Sound, new DreamMetaObjectSound());
            DreamObjectTree.SetMetaObject(DreamPath.Image, new DreamMetaObjectImage());
            DreamObjectTree.SetMetaObject(DreamPath.World, new DreamMetaObjectWorld());
            DreamObjectTree.SetMetaObject(DreamPath.Client, new DreamMetaObjectClient());
            DreamObjectTree.SetMetaObject(DreamPath.Datum, new DreamMetaObjectDatum());
            DreamObjectTree.SetMetaObject(DreamPath.Regex, new DreamMetaObjectRegex());
            DreamObjectTree.SetMetaObject(DreamPath.Atom, new DreamMetaObjectAtom());
            DreamObjectTree.SetMetaObject(DreamPath.Area, new DreamMetaObjectArea());
            DreamObjectTree.SetMetaObject(DreamPath.Turf, new DreamMetaObjectTurf());
            DreamObjectTree.SetMetaObject(DreamPath.Movable, new DreamMetaObjectMovable());
            DreamObjectTree.SetMetaObject(DreamPath.Mob, new DreamMetaObjectMob());
            DreamProcNative.SetupNativeProcs();
            
            TickStartTime = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();

            WorldInstance = DreamObjectTree.CreateObject(DreamPath.World);
            DreamObjectTree.GetObjectDefinitionFromPath(DreamPath.Root).GlobalVariables["world"].Value = new DreamValue(WorldInstance);

            if (CompiledJson.GlobalInitProc != null) {
                DreamProc globalInitProc = new DreamProc(CompiledJson.GlobalInitProc.Bytecode);
                globalInitProc.Run(WorldInstance, new DreamProcArguments(new(), new()));
            }

            DreamMap = new DreamMap();
            DreamMap.LoadMap(CompiledJson.Maps[0]);

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
                    connection.UpdateStat();

                    if (connection.PressedKeys.Contains(38)) {
                        Task.Run(() => connection.ClientDreamObject?.CallProc("North"));
                    } else if (connection.PressedKeys.Contains(39)) {
                        Task.Run(() => connection.ClientDreamObject?.CallProc("East"));
                    } else if (connection.PressedKeys.Contains(40)) {
                        Task.Run(() => connection.ClientDreamObject?.CallProc("South"));
                    } else if (connection.PressedKeys.Contains(37)) {
                        Task.Run(() => connection.ClientDreamObject?.CallProc("West"));
                    }
                }

                DreamStateManager.FinalizeCurrentDeltaState();
                DreamServer.Process();

                TickCount++;

                int elapsedTime = (int)(new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds() - TickStartTime);
                int tickLength = (int)(100 * WorldInstance.GetVariable("tick_lag").GetValueAsNumber());
                int timeToSleep = tickLength - elapsedTime;
                if (timeToSleep > 0) Thread.Sleep(timeToSleep);
            }
        }

        private static bool LoadCompiledDreamJson(string filepath) {
            DreamResource compiledJsonResource = DreamResourceManager.LoadResource(filepath);
            CompiledJson = JsonSerializer.Deserialize<DreamCompiledJson>(compiledJsonResource.ReadAsString());

            if (CompiledJson.Maps == null || CompiledJson.Maps.Count == 0) {
                Console.WriteLine("The game does not include a map");

                return false;
            } else if (CompiledJson.Maps.Count > 1) {
                Console.WriteLine("The game includes more than one map");

                return false;
            }

            if (CompiledJson.Interface == null) {
                Console.WriteLine("The game does not include an interface file");

                return false;
            }

            return true;
        }


        private static bool LoadInterface(string filepath) {
            DreamResource interfaceResource = DreamResourceManager.LoadResource(filepath);
            DMFLexer dmfLexer = new DMFLexer(filepath, interfaceResource.ReadAsString());
            DMFParser dmfParser = new DMFParser(dmfLexer);

            _clientInterface = dmfParser.Interface();

            if (dmfParser.Errors.Count > 0) {
                Console.WriteLine("Errors while parsing the interface file");

                foreach (CompilerError error in dmfParser.Errors) {
                    Console.WriteLine(error);
                }

                return false;
            }

            return true;
        }

        private static void RegisterPacketCallbacks() {
            DreamServer.RegisterPacketCallback<PacketRequestResource>(PacketID.RequestResource, DreamResourceManager.HandleRequestResourcePacket);
            DreamServer.RegisterPacketCallback(PacketID.KeyboardInput, (DreamConnection connection, PacketKeyboardInput pKeyboardInput) => connection.HandlePacketKeyboardInput(pKeyboardInput));
            DreamServer.RegisterPacketCallback(PacketID.ClickAtom, (DreamConnection connection, PacketClickAtom pClickAtom) => connection.HandlePacketClickAtom(pClickAtom));
            DreamServer.RegisterPacketCallback(PacketID.Topic, (DreamConnection connection, PacketTopic pTopic) => connection.HandlePacketTopic(pTopic));
            DreamServer.RegisterPacketCallback(PacketID.PromptResponse, (DreamConnection connection, PacketPromptResponse pPromptResponse) => connection.HandlePacketPromptResponse(pPromptResponse));
            DreamServer.RegisterPacketCallback(PacketID.CallVerb, (DreamConnection connection, PacketCallVerb pCallVerb) => connection.HandlePacketCallVerb(pCallVerb));
        }

        private static void OnDeltaStateFinalized(DreamDeltaState deltaState) {
            foreach (DreamConnection dreamConnection in DreamServer.DreamConnections) {
                dreamConnection.SendPacket(new PacketDeltaGameState(deltaState, dreamConnection.CKey));
            }
        }

        private static void OnDreamConnectionRequest(DreamConnection connection) {
            Console.WriteLine("Connection request from '" + connection.CKey + "'");
            DreamStateManager.AddClient(connection.CKey);

            connection.ClientDreamObject = DreamObjectTree.CreateObject(DreamPath.Client, new DreamProcArguments(new List<DreamValue>() { DreamValue.Null }));
            ClientToConnection[connection.ClientDreamObject] = connection;
            connection.SendPacket(new PacketInterfaceData(_clientInterface));
            connection.SendPacket(new PacketFullGameState(DreamStateManager.FullState));

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
