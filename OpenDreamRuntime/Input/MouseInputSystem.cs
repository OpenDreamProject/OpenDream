using System.Collections.Specialized;
using System.Web;
using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Input;

namespace OpenDreamRuntime.Input;

internal sealed class MouseInputSystem : SharedMouseInputSystem {
    [Dependency] private readonly AtomManager _atomManager = default!;
    [Dependency] private readonly DreamManager _dreamManager = default!;
    [Dependency] private readonly IDreamMapManager _mapManager = default!;

    public override void Initialize() {
        base.Initialize();

        SubscribeNetworkEvent<AtomClickedEvent>(OnAtomClicked);
        SubscribeNetworkEvent<AtomDraggedEvent>(OnAtomDragged);
        SubscribeNetworkEvent<StatClickedEvent>(OnStatClicked);
    }

    private void OnAtomClicked(AtomClickedEvent e, EntitySessionEventArgs sessionEvent) {
        var connection = _dreamManager.GetConnectionBySession(sessionEvent.SenderSession);
        var clicked = _dreamManager.GetFromClientReference(connection, e.ClickedAtom);
        if (clicked is not DreamObjectAtom atom)
            return;

        HandleAtomClick(e, atom, sessionEvent);
    }

    private void OnAtomDragged(AtomDraggedEvent e, EntitySessionEventArgs sessionEvent) {
        var connection = _dreamManager.GetConnectionBySession(sessionEvent.SenderSession);
        var src = _dreamManager.GetFromClientReference(connection, e.SrcAtom);
        if (src is not DreamObjectAtom srcAtom)
            return;

        var usr = connection.Mob;
        var srcPos = _atomManager.GetAtomPosition(srcAtom);
        var over = (e.OverAtom != null)
            ? _dreamManager.GetFromClientReference(connection, e.OverAtom.Value) as DreamObjectAtom
            : null;

        _mapManager.TryGetTurfAt((srcPos.X, srcPos.Y), srcPos.Z, out var srcLoc);

        DreamValue overLocValue = DreamValue.Null;
        if (over != null) {
            var overPos = _atomManager.GetAtomPosition(over);

            _mapManager.TryGetTurfAt((overPos.X, overPos.Y), overPos.Z, out var overLoc);
            overLocValue = new(overLoc);
        }

        connection.Client?.SpawnProc("MouseDrop", usr: usr,
            new DreamValue(src),
            new DreamValue(over),
            new DreamValue(srcLoc), // TODO: Location can be a skin element
            overLocValue,
            DreamValue.Null, // TODO: src_control and over_control
            DreamValue.Null,
            new DreamValue(ConstructClickParams(e.Params)));
    }

    private void OnStatClicked(StatClickedEvent e, EntitySessionEventArgs sessionEvent) {
        if (!_dreamManager.LocateRef(e.AtomRef).TryGetValueAsDreamObject<DreamObjectAtom>(out var dreamObject))
            return;

        HandleAtomClick(e, dreamObject, sessionEvent);
    }

    private void HandleAtomClick(IAtomMouseEvent e, DreamObjectAtom atom, EntitySessionEventArgs sessionEvent) {
        var session = sessionEvent.SenderSession;
        var connection = _dreamManager.GetConnectionBySession(session);
        var usr = connection.Mob;

        connection.Client?.SpawnProc("Click", usr: usr,
            new DreamValue(atom),
            DreamValue.Null,
            DreamValue.Null,
            new DreamValue(ConstructClickParams(e.Params)));
    }

    private string ConstructClickParams(ClickParams clickParams) {
        NameValueCollection paramsBuilder = HttpUtility.ParseQueryString(string.Empty);
        if (clickParams.Middle) paramsBuilder.Add("middle", "1");
        if (clickParams.Shift) paramsBuilder.Add("shift", "1");
        if (clickParams.Ctrl) paramsBuilder.Add("ctrl", "1");
        if (clickParams.Alt) paramsBuilder.Add("alt", "1");
        paramsBuilder.Add("screen-loc", clickParams.ScreenLoc.ToString());
        paramsBuilder.Add("icon-x", clickParams.IconX.ToString());
        paramsBuilder.Add("icon-y", clickParams.IconY.ToString());

        return paramsBuilder.ToString();
    }
}
