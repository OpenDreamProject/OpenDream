using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Rendering;

public sealed class ServerDreamParticlesSystem : SharedDreamParticlesSystem {
    public void MarkDirty(Entity<DreamParticlesComponent> ent){
        Dirty(ent, ent.Comp);
    }
}
