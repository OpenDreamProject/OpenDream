using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using OpenDreamShared.Rendering;
using Robust.Shared.Map;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectParticles : DreamObject {
    public EntityUid Entity = EntityUid.Invalid;
    public DreamParticlesComponent ParticlesComponent;

    public DreamObjectParticles(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        Entity = EntityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace)); //spawning an entity in nullspace means it never actually gets sent to any clients until it's placed on the map, or it gets a PVS override
        ParticlesComponent = EntityManager.AddComponent<DreamParticlesComponent>(Entity);
        //populate component with settings from type
        //do set/get var to grab those also
        //check if I need to manually send update events to the component?
        //add entity array to appearance objects
        //collect entities client-side for the rendermetadata
        //idk I guess bodge generators right now?
        //set up a special list type on /atom for /particles

    }
}
