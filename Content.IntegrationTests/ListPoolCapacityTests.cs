#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamShared;

namespace Content.IntegrationTests;

[TestFixture, NonParallelizable, Explicit("Isolated production list pool retention and reuse")]
public sealed class ListPoolCapacityTests : ContentIntegrationTest {
    private static readonly FieldInfo ValuesField = typeof(DreamList).GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo ItemsField = typeof(List<DreamValue>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly Stack<List<DreamValue>> Pool = (Stack<List<DreamValue>>)typeof(DreamList).GetField("ListPool", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    private static List<DreamValue> Values(DreamList list) => (List<DreamValue>)ValuesField.GetValue(list)!;
    private static void Collect() {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private async Task InWorld(Action<DreamManager, DreamObjectTree> check) {
        var temp = Path.Combine(Path.GetTempPath(), "list-pool-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var oldDirectory = Directory.GetCurrentDirectory();
        try {
            var fixture = Path.Combine(temp, "fixture.dm");
            File.WriteAllText(fixture, """
                /world/New()
                    return
                /proc/list_pool_semantics()
                    var/list/L = new/list(65537)
                    L[1] = "head"
                    L[65537] = "tail"
                    var/list/C = L.Copy()
                    L[1] = "changed"
                    C.Cut(2, 65537)
                    if(C.len != 2 || C[1] != "head" || C[2] != "tail" || L.len != 65537)
                        return 0
                    var/text = ""
                    for(var/value in C)
                        text += value
                    return text == "headtail"
                """);
            var compiler = new DMCompiler.DMCompiler();
            Assert.That(compiler.Compile(new() { Files = [fixture], StoreMessages = true }), Is.True, string.Join("\n", compiler.CompilerMessages));
            var options = new ServerContentIntegrationOption();
            options.CVarOverrides[OpenDreamCVars.JsonPath.Name] = Path.ChangeExtension(fixture, "json");
            options.CVarOverrides[OpenDreamCVars.TopicPort.Name] = "0";
            var server = StartServer(options);
            await server.WaitIdleAsync();
            await server.WaitRunTicks(1);
            await server.WaitAssertion(() => {
                var manager = server.ResolveDependency<DreamManager>();
                var tree = server.ResolveDependency<DreamObjectTree>();
                Assert.That(manager.ListPoolThreshold, Is.EqualTo(2048));
                Assert.That(manager.ListPoolSize, Is.EqualTo(256));
                var saved = Pool.ToArray();
                Pool.Clear();
                try {
                    check(manager, tree);
                    Assert.That(manager.LastDMException, Is.Null);
                } finally {
                    Pool.Clear();
                    foreach (var list in saved.Reverse()) Pool.Push(list);
                    manager.ListPoolSize = 256;
                }
            });
        } finally {
            Directory.SetCurrentDirectory(oldDirectory);
            Directory.Delete(temp, recursive: true);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] RetireBurst(DreamObjectTree tree) {
        var lists = Enumerable.Range(0, 32).Select(_ => tree.CreateList(131072)).ToArray();
        var arrays = lists.Select(list => new WeakReference(ItemsField.GetValue(Values(list))!)).ToArray();
        foreach (var list in lists) {
            list.Resize(131072);
            list.DecRef();
            Assert.That(list.Deleted, Is.True);
        }
        return arrays;
    }

    [Test]
    public async Task RetiredCapacityAndWeakRelease() => await InWorld((_, tree) => {
        var arrays = RetireBurst(tree);
        Collect();
        var alive = arrays.Count(array => array.IsAlive);
        var slots = Pool.Sum(list => (long)list.Capacity);
        TestContext.Out.WriteLine("POOL_RETENTION " + JsonSerializer.Serialize(new {
            count = Pool.Count, slots, maximum = Pool.Select(list => list.Capacity).DefaultIfEmpty().Max(),
            alive, retired = arrays.Length, valueBytes = Unsafe.SizeOf<DreamValue>(),
            backingPayloadBytes = slots * Unsafe.SizeOf<DreamValue>(), runtime = Environment.Version.ToString(), os = Environment.OSVersion.ToString()
        }));
        Assert.That(Pool.All(list => list.Capacity <= 65536), Is.True, "POOL_CAPACITY_BOUND");
        Assert.That(alive, Is.Zero, "POOL_CAPACITY_BOUND weak array release");
        foreach (var size in new[] { 2048, 2049, 65535, 65536, 65537, 350000 }) {
            Pool.Clear();
            var list = tree.CreateList(size);
            list.Resize(size);
            Assert.That(list.GetLength(), Is.EqualTo(size), "live size is unlimited");
            list.DecRef();
            Assert.That(Pool.Count, Is.EqualTo(size is > 2048 and <= 65536 ? 1 : 0), "POOL_CAPACITY_BOUND boundary");
        }
    });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RetireAssociatedChild(DreamObjectTree tree) {
        var child = tree.CreateObject(tree.Datum);
        var weak = new WeakReference(child);
        var list = tree.CreateList(4096);
        list.AddValue(new DreamValue(child));
        list.SetValue(new DreamValue("key"), new DreamValue(child));
        Assert.That(child.RefCount, Is.EqualTo(3));
        var copy = (DreamList)list.CreateCopy();
        Assert.That(child.RefCount, Is.EqualTo(5));
        Assert.That(copy.EnumerateValues().Select(value => value.Type), Is.EqualTo(new[] { DreamValue.DreamValueType.DreamObject, DreamValue.DreamValueType.String }));
        using (var value = copy.GetValue(new DreamValue("key"))) {
            Assert.That(value.MustGetValueAsDreamObject(), Is.SameAs(child));
            Assert.That(child.RefCount, Is.EqualTo(6));
        }
        copy.Cut(1, 2);
        Assert.That(copy.GetLength(), Is.EqualTo(1));
        Assert.That(child.RefCount, Is.EqualTo(4));
        list.DecRef();
        Assert.That(child.RefCount, Is.EqualTo(2));
        copy.DecRef();
        Assert.That(child.RefCount, Is.EqualTo(1));
        child.DecRef();
        Assert.That(child.Deleted, Is.True);
        return weak;
    }

    [Test]
    public async Task SemanticsReferencesAndMediumReuse() => await InWorld((manager, tree) => {
        var refs = tree.List.ObjectDefinition.DreamRefManager;
        var beforeLists = refs.GetCountOf(RefType.DreamObjectList);
        Assert.That(tree.TryGetGlobalProc("list_pool_semantics", out var proc), Is.True);
        using (var result = DreamThread.Run(proc!, manager.WorldInstance, null)) {
            Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(1));
        }
        Assert.That(refs.GetCountOf(RefType.DreamObjectList), Is.EqualTo(beforeLists), "compiled Copy/Cut/iteration releases lists");
        Pool.Clear();
        var child = RetireAssociatedChild(tree);
        Collect();
        TestContext.Out.WriteLine("POOL_CHILD " + JsonSerializer.Serialize(new { count = Pool.Count, childAlive = child.IsAlive, entries = Pool.Select(list => list.Count).ToArray() }));
        Assert.That(Pool.Count, Is.EqualTo(1), "POOL_MEDIUM_REUSE");
        Assert.That(Pool.Peek().Count, Is.Zero, "POOL_CLEAR_AND_RELEASE");
        Assert.That(child.IsAlive, Is.False, "POOL_CLEAR_AND_RELEASE child managed reference");
        Pool.Clear();
        var first = tree.CreateList(4096);
        var second = tree.CreateList(8192);
        var firstValues = Values(first);
        var secondValues = Values(second);
        first.AddValue(new DreamValue(1));
        second.AddValue(new DreamValue(2));
        first.DecRef();
        second.DecRef();
        var rent = tree.CreateList(4096);
        var rent2 = tree.CreateList(4096);
        Assert.That(Values(rent), Is.SameAs(secondValues), "POOL_MEDIUM_REUSE LIFO");
        Assert.That(Values(rent2), Is.SameAs(firstValues), "POOL_MEDIUM_REUSE LIFO");
        Assert.That(rent.GetLength(), Is.Zero);
        Assert.That(rent2.GetLength(), Is.Zero);
        rent.DecRef();
        rent2.DecRef();
        Pool.Clear();
        manager.ListPoolSize = 2;
        var three = Enumerable.Range(0, 3).Select(_ => tree.CreateList(4096)).ToArray();
        foreach (var list in three) list.DecRef();
        Assert.That(Pool.Count, Is.EqualTo(2), "existing count limit");
    });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Cycle(DreamObjectTree tree, int size) {
        var list = tree.CreateList(size);
        list.Resize(size);
        list.DecRef();
    }

    [Test]
    public async Task ComparableWorkloads() => await InWorld((_, tree) => {
        foreach (var size in new[] { 32, 4096, 65536, 65537, 131072, 350000 }) {
            Pool.Clear();
            for (var i = 0; i < 2; i++) Cycle(tree, size);
            var generations = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            for (var i = 0; i < 8; i++) Cycle(tree, size);
            timer.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            TestContext.Out.WriteLine("POOL_METRIC " + JsonSerializer.Serialize(new {
                size, count = 8, allocated, seconds = timer.Elapsed.TotalSeconds,
                collections = generations.Select((before, generation) => GC.CollectionCount(generation) - before).ToArray(),
                poolCount = Pool.Count, slots = Pool.Sum(list => (long)list.Capacity), valueBytes = Unsafe.SizeOf<DreamValue>(), runtime = Environment.Version.ToString()
            }));
        }
        Pool.Clear();
        Cycle(tree, 350000);
        var beforeMedium = Pool.Sum(list => (long)list.Capacity);
        Cycle(tree, 4096);
        TestContext.Out.WriteLine("POOL_MIXED " + JsonSerializer.Serialize(new {
            beforeMedium, afterMedium = Pool.Sum(list => (long)list.Capacity), poolCount = Pool.Count
        }));
    });
}
