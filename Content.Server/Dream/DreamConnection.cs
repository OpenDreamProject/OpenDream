using System.Collections.Generic;
using Content.Server.DM;
using Content.Shared.Dream;
using Content.Shared.Network.Messages;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Content.Server.Dream
{
    public class DreamConnection
    {
        [Dependency] private readonly IServerNetManager _netManager;
        [Dependency] private readonly IDreamManager _dreamManager;
        [Dependency] private readonly IAtomManager _atomManager;

        [ViewVariables] private Dictionary<string, DreamProc> _availableVerbs = new();
        [ViewVariables] private Dictionary<string, List<string>> _statPanels = new();
        [ViewVariables] private bool _currentlyUpdatingStat;
        [ViewVariables] public IPlayerSession Session { get; }

        [ViewVariables] public DreamObject ClientDreamObject;

        [ViewVariables] private DreamObject _mobDreamObject;

        [ViewVariables] private string _outputStatPanel;
        [ViewVariables] private string _selectedStatPanel;

        public string SelectedStatPanel {
            get => _selectedStatPanel;
            set {
                _selectedStatPanel = value;
                var msg = _netManager.CreateNetMessage<MsgSelectStatPanel>();
                msg.StatPanel = value;
                Session.ConnectedClient.SendMessage(msg);
            }
        }

        public DreamObject MobDreamObject
        {
            get => _mobDreamObject;
            set
            {
                if (_mobDreamObject != value)
                {
                    if (_mobDreamObject != null) _mobDreamObject.SpawnProc("Logout");

                    if (value != null && value.IsSubtypeOf(DreamPath.Mob))
                    {
                        DreamConnection oldMobConnection = _dreamManager.GetConnectionFromMob(value);
                        if (oldMobConnection != null) oldMobConnection.MobDreamObject = null;

                        _mobDreamObject = value;
                        ClientDreamObject?.SetVariable("eye", new DreamValue(_mobDreamObject));
                        _mobDreamObject.SpawnProc("Login");
                        Session.AttachToEntity(_atomManager.GetAtomEntity(_mobDreamObject));
                    }
                    else
                    {
                        Session.DetachFromEntity();
                        _mobDreamObject = null;
                    }

                    UpdateAvailableVerbs();
                }
            }
        }

        public DreamConnection(IPlayerSession session)
        {
            IoCManager.InjectDependencies(this);

            Session = session;
        }

        public void UpdateAvailableVerbs()
        {
            /*
            _availableVerbs.Clear();

            if (MobDreamObject != null)
            {
                List<DreamValue> mobVerbPaths = MobDreamObject.GetVariable("verbs").GetValueAsDreamList().GetValues();

                foreach (DreamValue mobVerbPath in mobVerbPaths)
                {
                    DreamPath path = mobVerbPath.GetValueAsPath();

                    _availableVerbs.Add(path.LastElement, MobDreamObject.GetProc(path.LastElement));
                }
            }

            var msg = _netManager.CreateNetMessage<MsgUpdateAvailableVerbs>();
            msg.AvailableVerbs = _availableVerbs.Keys.ToArray();
            Session.ConnectedClient.SendMessage(msg);
            */
        }

        public void UpdateStat()
        {
            if (_currentlyUpdatingStat)
                return;

            _currentlyUpdatingStat = true;
            _statPanels.Clear();

            DreamThread.Run(async (state) =>
            {
                try
                {
                    var statProc = ClientDreamObject.GetProc("Stat");

                    await state.Call(statProc, ClientDreamObject, _mobDreamObject, new DreamProcArguments(null));
                    if (Session.Status == SessionStatus.InGame)
                    {
                        var msg = _netManager.CreateNetMessage<MsgUpdateStatPanels>();
                        msg.StatPanels = _statPanels;
                        Session.ConnectedClient.SendMessage(msg);
                    }

                    return DreamValue.Null;
                }
                finally
                {
                    _currentlyUpdatingStat = false;
                }
            });
        }

        public void SetOutputStatPanel(string name)
        {
            if (!_statPanels.ContainsKey(name)) _statPanels.Add(name, new List<string>());

            _outputStatPanel = name;
        }

        public void AddStatPanelLine(string text)
        {
            _statPanels[_outputStatPanel].Add(text);
        }

        public void HandlePacketSelectStatPanel(MsgSelectStatPanel message)
        {
            _selectedStatPanel = message.StatPanel;
        }
    }
}
