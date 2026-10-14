#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Map;
using OpenDreamShared;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests;

[TestFixture, NonParallelizable, Explicit("Requires an isolated real-grid integration host")]
public sealed class TurfTileQueueTests : ContentIntegrationTest {
    [Test]
    public async Task PendingTilesPreserveEmptyTransitions() {
        var original = Directory.GetCurrentDirectory();
        var temporary = Path.Combine(Path.GetTempPath(), "opendream-tile-queue-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try {
            var fixture = Path.Combine(temporary, "fixture.dm");
            File.WriteAllText(fixture, "/world\n    maxx = 16\n    maxy = 16\n    maxz = 2\n");
            var compiler = new DMCompiler.DMCompiler();
            Assert.That(compiler.Compile(new() { Files = [fixture], StoreMessages = true }), Is.True,
                "Isolated DM fixture compilation failed: " + string.Join("\n", compiler.CompilerMessages));
            var options = new ServerContentIntegrationOption();
            options.CVarOverrides[OpenDreamCVars.JsonPath.Name] = Path.ChangeExtension(fixture, "json");
            options.CVarOverrides[OpenDreamCVars.TopicPort.Name] = "0";
            var server = StartServer(options);
            await server.WaitIdleAsync();
            await server.WaitRunTicks(1);
            await server.WaitAssertion(() => Check(server.ResolveDependency<IDreamMapManager>(),
                server.ResolveDependency<DreamManager>(), server.ResolveDependency<IEntityManager>(),
                server.ResolveDependency<IEntitySystemManager>()));
        } finally {
            Directory.SetCurrentDirectory(original);
            Directory.Delete(temporary, true);
        }
    }

    private static List<(Vector2i, Tile)> Queue(IDreamMapManager.Level level) =>
        (List<(Vector2i, Tile)>)typeof(IDreamMapManager.Level).GetField("QueuedTileUpdates")!.GetValue(level)!;

    private static void Add(IDreamMapManager.Level level, Vector2i position, Tile tile) {
        var method = typeof(IDreamMapManager.Level).GetMethod("QueueTileUpdate");
        if (method == null) Queue(level).Add((position, tile));
        else method.Invoke(level, new object[] { (position, tile) });
    }

    private static void Check(IDreamMapManager manager, DreamManager dream, IEntityManager entities,
        IEntitySystemManager systems) {
        var levels = (List<IDreamMapManager.Level>)typeof(DreamMapManager)
            .GetField("_levels", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager)!;
        var map = systems.GetEntitySystem<SharedMapSystem>();
        var transforms = systems.GetEntitySystem<SharedTransformSystem>();
        Assert.That(dream.LastDMException, Is.Null);
        manager.UpdateTiles();
        var peak = 0;
        var retained = 0;
        for (var pass = 0; pass < 3; pass++) {
            Assert.That(manager.TryGetTurfAt((1, 1), 1, out var turf), Is.True);
            for (var i = 0; i < 20000; i++) {
                using var appearance = MutableAppearance.Get();
                appearance.IconState = (i % 2).ToString();
                manager.SetTurfAppearance(turf!, appearance);
            }
            peak = Math.Max(peak, Queue(levels[0]).Count);
            manager.UpdateTiles();
            Assert.That(map.GetTileRef(levels[0].Grid, (1, 1)).Tile.TypeId, Is.EqualTo((int)turf!.Appearance.MustGetId()));
            retained = Math.Max(retained, Queue(levels[0]).Capacity);
            Assert.That(Queue(levels[0]).Count, Is.Zero);
        }
        // No-update calls must not allocate or grow a queue.
        var idle = Queue(levels[0]);
        for (var i = 0; i < 20; i++) manager.UpdateTiles();
        Assert.That(Queue(levels[0]), Is.SameAs(idle));
        // Unique coordinates are an unchanged lower bound, with separate per-level queues.
        foreach (var level in levels) {
            for (var x = 1; x <= 16; x++) for (var y = 1; y <= 16; y++) {
                Assert.That(manager.TryGetTurfAt((x, y), level.Z, out var turf), Is.True);
                using var appearance = MutableAppearance.Get();
                appearance.IconState = "unique";
                manager.SetTurfAppearance(turf!, appearance);
            }
            Assert.That(Queue(level).Count, Is.EqualTo(256));
        }
        manager.UpdateTiles();
        for (var pass = 0; pass < 8; pass++) {
            using var appearance = MutableAppearance.Get();
            appearance.IconState = "area" + pass;
            manager.SetAreaAppearance(manager.DefaultArea, appearance);
        }
        var areaPeak = levels.Sum(l => Queue(l).Count);
        manager.UpdateTiles();
        foreach (var level in levels) for (var x = 1; x <= 16; x++) for (var y = 1; y <= 16; y++) {
            manager.TryGetTurfAt((x, y), level.Z, out var turf);
            Assert.That(map.GetTileRef(level.Grid, (x, y)).Tile.TypeId, Is.EqualTo((int)turf!.Appearance.MustGetId()));
        }
        // Actual Robust grid and transform system, compared with the original uncoalesced stream.
        var position = new Vector2i(4, 4);
        var stream = new List<(Vector2i, Tile)> {
            (position, new Tile(1)), (position, new Tile(2)), (position, Tile.Empty),
            (position, new Tile(1)), (position, new Tile(2))
        };
        var anchorStates = new List<bool>();
        foreach (var level in levels) {
            map.SetTile(level.Grid, position, new Tile(1));
            var entity = entities.SpawnEntity(null, new EntityCoordinates(level.Grid.Owner, new Vector2(4.5f, 4.5f)));
            var transform = entities.GetComponent<TransformComponent>(entity);
            Assert.That(transforms.AnchorEntity((entity, transform), level.Grid, position), Is.True);
            Assert.That(transform.Anchored, Is.True);
            if (level.Z == 1) {
                foreach (var (pos, tile) in stream) Add(level, pos, tile);
                manager.UpdateTiles();
            } else map.SetTiles(level.Grid, stream);
            Assert.That(map.GetTileRef(level.Grid, position).Tile, Is.EqualTo(new Tile(2)));
            anchorStates.Add(!transform.Anchored && transform.ParentUid != level.Grid.Owner);
            entities.DeleteEntity(entity);
        }
        // Empty->nonempty->empty must retain the middle tile, otherwise Robust sees no empty change.
        foreach (var level in levels) {
            map.SetTile(level.Grid, position, Tile.Empty);
            Add(level, position, new Tile(1)); Add(level, position, new Tile(2));
            Add(level, position, Tile.Empty); Add(level, position, new Tile(1));
            Add(level, position, Tile.Empty);
        }
        var barriers = levels.All(l => Queue(l).Count >= 4);
        manager.UpdateTiles();
        Assert.That(levels.All(l => map.GetTileRef(l.Grid, position).Tile.IsEmpty), Is.True);
        // Callback writes survive successful drains; failure restores original before callback writes.
        var flush = typeof(IDreamMapManager.Level).GetMethod("FlushTileUpdates");
        if (flush != null) {
            var level = levels[0];
            Add(level, position, new Tile(1));
            flush.Invoke(level, new object[] { (Action<List<(Vector2i, Tile)>>)(_ => {
                Add(level, position, Tile.Empty);
                flush.Invoke(level, new object[] { (Action<List<(Vector2i, Tile)>>)(_ => Assert.Fail("recursive drain")) });
            }) });
            Assert.That(Queue(level).Select(t => t.Item2), Is.EqualTo(new[] { Tile.Empty }));
            manager.UpdateTiles();
            Add(level, position, new Tile(1));
            Assert.Throws<TargetInvocationException>(() => flush.Invoke(level, new object[] {
                (Action<List<(Vector2i, Tile)>>)(_ => { Add(level, (5, 5), Tile.Empty); throw new InvalidOperationException("injected apply failure"); }) }));
            Assert.That(Queue(level).Select(t => t.Item2), Is.EqualTo(new[] { new Tile(1), Tile.Empty }));
            manager.UpdateTiles();
        }
        Assert.That(dream.LastDMException, Is.Null);
        TestContext.Out.WriteLine($"TILE_QUEUE_OBSERVATION: repeated=60000 peak={peak} retained={retained} area_peak={areaPeak}");
        TestContext.Out.WriteLine($"TILE_QUEUE_PARITY: anchored_empty={anchorStates[0]} baseline_empty={anchorStates[1]} barriers={barriers}");
        TestContext.Out.WriteLine("TILE_QUEUE_COMPLETE: production routes, real grids, transforms, levels, drains and final tiles checked");
        Assert.Multiple(() => {
            Assert.That(peak, Is.EqualTo(1), "TILE_QUEUE_BOUNDED");
            Assert.That(retained, Is.Zero, "TILE_QUEUE_RELEASED");
            Assert.That(areaPeak, Is.EqualTo(512), "TILE_QUEUE_AREA_BOUNDED");
            Assert.That(anchorStates[0] && anchorStates[1] && barriers, Is.True, "TILE_QUEUE_EMPTY_BARRIERS");
        });
    }
}
