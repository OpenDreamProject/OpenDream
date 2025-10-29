using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class VariablePresentationHint {
    /**
     * The kind of variable. Before introducing additional values, try to use the
     * listed values.
     * Values:
     * 'property': Indicates that the object is a property.
     * 'method': Indicates that the object is a method.
     * 'class': Indicates that the object is a class.
     * 'data': Indicates that the object is data.
     * 'event': Indicates that the object is an event.
     * 'baseClass': Indicates that the object is a base class.
     * 'innerClass': Indicates that the object is an inner class.
     * 'interface': Indicates that the object is an interface.
     * 'mostDerivedClass': Indicates that the object is the most derived class.
     * 'virtual': Indicates that the object is virtual, that means it is a
     * synthetic object introduced by the adapter for rendering purposes, e.g. an
     * index range for large arrays.
     * 'dataBreakpoint': Deprecated: Indicates that a data breakpoint is
     * registered for the object. The `hasDataBreakpoint` attribute should
     * generally be used instead.
     * etc.
     */
    [JsonPropertyName("kind")] public string? Kind { get; set; }

    public const string KindProperty = "property";
    public const string KindMethod = "method";
    public const string KindClass = "class";
    public const string KindData = "data";
    public const string KindEvent = "event";
    public const string KindBaseClass = "baseClass";
    public const string KindInnerClass = "innerClass";
    public const string KindInterface = "interface";
    public const string KindMostDerivedClass = "mostDerivedClass";
    public const string KindVirtual = "virtual";
    public const string KindDataBreakpoint = "dataBreakpoint";

    /**
     * Set of attributes represented as an array of strings. Before introducing
     * additional values, try to use the listed values.
     * Values:
     * 'static': Indicates that the object is static.
     * 'constant': Indicates that the object is a constant.
     * 'readOnly': Indicates that the object is read only.
     * 'rawString': Indicates that the object is a raw string.
     * 'hasObjectId': Indicates that the object can have an Object ID created for
     * it.
     * 'canHaveObjectId': Indicates that the object has an Object ID associated
     * with it.
     * 'hasSideEffects': Indicates that the evaluation had side effects.
     * 'hasDataBreakpoint': Indicates that the object has its value tracked by a
     * data breakpoint.
     * etc.
     */
    [JsonPropertyName("attributes")] public IEnumerable<string>? Attributes { get; set; }

    public const string AttributeStatic = "static";
    public const string AttributeConstant = "constant";
    public const string AttributeReadOnly = "readOnly";
    public const string AttributeRawString = "rawString";
    public const string AttributeHasObjectId = "hasObjectId";
    public const string AttributeCanHaveObjectId = "canHaveObjectId";
    public const string AttributeHasSideEffects = "hasSideEffects";
    public const string AttributeHasDataBreakpoint = "hasDataBreakpoint";

    /**
     * Visibility of variable. Before introducing additional values, try to use
     * the listed values.
     * Values: 'public', 'private', 'protected', 'internal', 'final', etc.
     */
    [JsonPropertyName("visibility")] public string? Visiblity { get; set; }

    public const string VisibilityPublic = "public";
    public const string VisibilityPrivate = "private";
    public const string VisibilityProtected = "protected";
    public const string VisibilityInternal = "internal";
    public const string VisibilityFinal = "final";

    /**
     * If true, clients can present the variable with a UI that supports a
     * specific gesture to trigger its evaluation.
     * This mechanism can be used for properties that require executing code when
     * retrieving their value and where the code execution can be expensive and/or
     * produce side-effects. A typical example are properties based on a getter
     * function.
     * Please note that in addition to the `lazy` flag, the variable's
     * `variablesReference` is expected to refer to a variable that will provide
     * the value through another `variable` request.
     */
    [JsonPropertyName("lazy")] public bool? Lazy { get; set; }
}
