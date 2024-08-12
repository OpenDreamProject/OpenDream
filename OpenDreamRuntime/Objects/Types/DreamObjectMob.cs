﻿using OpenDreamRuntime.Procs.Native;
using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectMob : DreamObjectMovable {
    public DreamConnection? Connection;
    public string? Key;

    public int SeeInvisible {
        get => _sightComponent.SeeInvisibility;
        private set {
            _sightComponent.SeeInvisibility = (sbyte)value;
            EntityManager.Dirty(_sightComponent);
        }
    }

    public SightFlags Sight {
        get => _sightComponent.Sight;
        private set {
            _sightComponent.Sight = value;
            EntityManager.Dirty(_sightComponent);
        }
    }

    private readonly DreamMobSightComponent _sightComponent;

    public DreamObjectMob(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        _sightComponent = EntityManager.AddComponent<DreamMobSightComponent>(Entity);

        objectDefinition.Variables["see_invisible"].TryGetValueAsInteger(out var seeVis);
        objectDefinition.Variables["sight"].TryGetValueAsInteger(out var sight);

        SeeInvisible = seeVis;
        Sight = (SightFlags)sight;
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "client":
                value = new(Connection?.Client);
                return true;
            case "key":
                value = (Key != null) ? new(Key) : DreamValue.Null;
                return true;
            case "ckey":
                value = (Key != null) ? new(DreamProcNativeHelpers.Ckey(Key)) : DreamValue.Null;
                return true;
            case "see_invisible":
                value = new(SeeInvisible);
                return true;
            case "sight":
                value = new((int)Sight);
                return true;
            default:
                return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "client":
                value.TryGetValueAsDreamObject<DreamObjectClient>(out var newClient);

                // An invalid client or a null does nothing here
                if (newClient != null) {
                    newClient.Connection.Mob = this;
                }

                break;
            case "ckey":
                if (!value.TryGetValueAsString(out Key)) { // TODO: Does the key get set to a player's un-canonized username?
                    if (Connection != null)
                        Connection.Mob = null;
                    break;
                }

                foreach (var connection in DreamManager.Connections) {
                    if (DreamProcNativeHelpers.Ckey(connection.Session!.Name) == Key) {
                        connection.Mob = this;
                        break;
                    }
                }

                break;
            case "key":
                if (!value.TryGetValueAsString(out Key)) {
                    if (Connection != null)
                        Connection.Mob = null;
                    break;
                }

                if (PlayerManager.TryGetSessionByUsername(Key, out var session)) {
                    var connection = DreamManager.GetConnectionBySession(session);

                    connection.Mob = this;
                }

                break;
            case "see_invisible":
                value.TryGetValueAsInteger(out int seeVis);

                SeeInvisible = seeVis;
                break;
            case "sight":
                value.TryGetValueAsInteger(out int sight);

                Sight = (SightFlags)sight;
                break;
            default:
                base.SetVar(varName, value);
                break;
        }
    }

    public override void OperatorOutput(DreamValue b) {
        Connection?.OutputDreamValue(b);
    }
}
