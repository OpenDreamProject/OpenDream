﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DMCompiler.Json;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Tests {
    public sealed class DummyDreamMapManager : IDreamMapManager {
        public Vector2i Size => Vector2i.Zero;
        public int Levels => 0;

        public void Initialize() { }

        public void UpdateTiles() { }

        public void LoadMaps(List<DreamMapJson>? maps) { }

        public void InitializeAtoms(List<DreamMapJson>? maps) { }

        public void SetTurf(DreamObjectTurf turf, DreamObjectDefinition type, DreamProcArguments creationArguments) { }

        public void SetTurfAppearance(DreamObjectTurf turf, IconAppearance appearance) { }

        public void SetArea(DreamObjectTurf turf, DreamObjectArea area) { }

        public bool TryGetCellFromTransform(TransformComponent transform, [NotNullWhen(true)] out IDreamMapManager.Cell? cell) {
            cell = null;
            return false;
        }

        public IDreamMapManager.Cell GetCellFromTurf(DreamObjectTurf turf) {
            throw null;
        }

        public bool TryGetCellAt(Vector2i pos, int z, out IDreamMapManager.Cell? cell) {
            cell = null;
            return false;
        }

        public bool TryGetTurfAt(Vector2i pos, int z, out DreamObjectTurf turf) {
            turf = null;
            return false;
        }

        public void SetZLevels(int levels) { }

        public EntityUid GetZLevelEntity(int z) {
            return EntityUid.Invalid;
        }
    }
}
