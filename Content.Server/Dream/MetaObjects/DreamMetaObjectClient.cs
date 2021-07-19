using Content.Server.DM;
using OpenDreamShared.Dream;
using Robust.Server.Player;
using Robust.Shared.IoC;

namespace Content.Server.Dream.MetaObjects {
    class DreamMetaObjectClient : DreamMetaObjectRoot {
        public override bool ShouldCallNew => true;

        private IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();
        private IAtomManager _atomManager = IoCManager.Resolve<IAtomManager>();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            ClientPerspective perspective = (ClientPerspective)dreamObject.GetVariable("perspective").GetValueAsInteger();
            if (perspective != ClientPerspective.Mob) {
                //Runtime.StateManager.AddClientPerspectiveDelta(connection.CKey, perspective);
            }
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue)
        {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            switch (variableName)
            {
                case "eye":
                {
                    string ckey = dreamObject.GetVariable("ckey").GetValueAsString();
                    DreamObject eye = variableValue.GetValueAsDreamObject();

                    //Runtime.StateManager.AddClientEyeIDDelta(ckey, eyeID);
                    break;
                }
                case "perspective":
                {
                    string ckey = dreamObject.GetVariable("ckey").GetValueAsString();

                    //Runtime.StateManager.AddClientPerspectiveDelta(ckey, (ClientPerspective)variableValue.GetValueAsInteger());
                    break;
                }
                case "mob":
                {
                    IPlayerSession session = _dreamManager.GetSessionFromClient(dreamObject);

                    if (variableValue.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out DreamObject mob)) {
                        _atomManager.GetAtomEntity(mob).GetComponent<Robust.Server.GameObjects.SpriteComponent>().DrawDepth = 10;
                        mob.SetVariable("icon", new("mob.png"));
                        session.AttachToEntity(_atomManager.GetAtomEntity(mob));
                        _dreamManager.c = mob;
                    } else {
                        session.DetachFromEntity();
                    }

                    break;
                }
                case "screen":
                {
                    /*if (oldVariableValue.TryGetValueAsDreamList(out DreamList oldList)) {
                        oldList.Cut();
                        oldList.ValueAssigned -= ScreenValueAssigned;
                        oldList.BeforeValueRemoved -= ScreenBeforeValueRemoved;
                        _screenListToClient.Remove(oldList);
                    }

                    DreamList screenList;
                    if (!variableValue.TryGetValueAsDreamList(out screenList)) {
                        screenList = DreamList.Create(Runtime);
                    }

                    screenList.ValueAssigned += ScreenValueAssigned;
                    screenList.BeforeValueRemoved += ScreenBeforeValueRemoved;
                    _screenListToClient[screenList] = dreamObject;*/
                    break;
                }
                case "statpanel":
                {
                    //DreamConnection connection = Runtime.Server.GetConnectionFromClient(dreamObject);

                    //connection.SelectedStatPanel = variableValue.GetValueAsString();
                    break;
                }
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue)
        {
            switch (variableName)
            {
                //TODO actually return the key
                case "key":
                case "ckey":
                {
                    return new(_dreamManager.GetSessionFromClient(dreamObject).Name);
                }
                case "mob":
                {
                    //var connection = Runtime.Server.GetConnectionFromClient(dreamObject);
                    //return new DreamValue(connection.MobDreamObject);
                    return DreamValue.Null;
                }
                case "address":
                {
                    return new(_dreamManager.GetSessionFromClient(dreamObject).ConnectedClient.RemoteEndPoint.Address.ToString());
                }
                case "inactivity":
                    return new DreamValue(0);
                case "timezone":
                {
                    //DreamConnection connection = Runtime.Server.GetConnectionFromClient(dreamObject);
                    //return new DreamValue((float)connection.ClientData.Timezone.BaseUtcOffset.TotalHours);
                    return new(0);
                }
                case "statpanel":
                {
                    //DreamConnection connection = Runtime.Server.GetConnectionFromClient(dreamObject);
                    //return new DreamValue(connection.SelectedStatPanel);
                    return DreamValue.Null;
                }
                case "connection":
                {
                    return new DreamValue("seeker");
                }
                default:
                    return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            //DreamConnection connection = Runtime.Server.GetConnectionFromClient(a.GetValueAsDreamObjectOfType(DreamPath.Client));

            //connection.OutputDreamValue(b);
            return new DreamValue(0);
        }

        //private void ScreenValueAssigned(DreamList screenList, DreamValue screenKey, DreamValue screenValue) {
        //    if (screenValue == DreamValue.Null) return;

        //    DreamObject atom = screenValue.GetValueAsDreamObjectOfType(DreamPath.Movable);
        //    Runtime.StateManager.AddClientScreenObject(_screenListToClient[screenList].GetVariable("ckey").GetValueAsString(), atom);
        //}

        //private void ScreenBeforeValueRemoved(DreamList screenList, DreamValue screenKey, DreamValue screenValue) {
        //    if (screenValue == DreamValue.Null) return;

        //    DreamObject atom = screenValue.GetValueAsDreamObjectOfType(DreamPath.Movable);
        //    Runtime.StateManager.RemoveClientScreenObject(_screenListToClient[screenList].GetVariable("ckey").GetValueAsString(), atom);
        //}
    }
}
