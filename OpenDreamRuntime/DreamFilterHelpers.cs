using System.Reflection;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime;

/// <summary>
/// Converts DM filter vars (DreamValues) directly onto <see cref="DreamFilter"/> classes.
/// </summary>
public static class DreamFilterHelpers {
    private static readonly Dictionary<Type, Dictionary<string, (FieldInfo Field, bool Required)>> FieldsByType = BuildFieldsByType();

    private static Dictionary<Type, Dictionary<string, (FieldInfo Field, bool Required)>> BuildFieldsByType() {
        var cache = new Dictionary<Type, Dictionary<string, (FieldInfo Field, bool Required)>>();
        foreach (var type in DreamFilter.AllTypes) {
            cache[type] = BuildFields(type);
        }

        return cache;
    }

    private static Dictionary<string, (FieldInfo Field, bool Required)> BuildFields(Type type) {
        var fields = new Dictionary<string, (FieldInfo, bool)>();
        for (Type? t = type; t != null && t != typeof(object); t = t.BaseType) {
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
                var dataField = field.GetCustomAttribute<DataFieldAttribute>();
                if (dataField?.Tag == null)
                    continue;

                fields[dataField.Tag] = (field, dataField.Required);
            }
        }

        return fields;
    }

    private static Dictionary<string, (FieldInfo Field, bool Required)> GetFields(Type filterType) => FieldsByType[filterType];

    /// <summary>
    /// Creates a new filter of the given type, applying defaults from field initializers,
    /// setting the provided vars, and enforcing required fields.
    /// </summary>
    public static DreamFilter Create(Type filterType, IEnumerable<(string Name, DreamValue Value)> vars) {
        var filter = (DreamFilter)Activator.CreateInstance(filterType)!;
        var provided = new HashSet<string>();

        foreach (var (name, value) in vars) {
            if (SetField(filter, name, value))
                provided.Add(name);
        }

        foreach (var (name, field) in GetFields(filterType)) {
            if (field.Required && !provided.Contains(name))
                throw new Exception($"Filter type \"{filter.FilterType}\" requires a value for \"{name}\"");
        }

        return filter;
    }

    /// <summary>
    /// Returns a copy of the filter with the var changed, or the same filter if the value is unchanged.
    /// The original is never modified, since other appearances may share it.
    /// </summary>
    /// <exception cref="DMException">The filter type has no such var, or the var is read-only</exception>
    public static DreamFilter SetVar(DreamFilter filter, string varName, DreamValue value) {
        if (varName is "type" or "name")
            throw new DMException($"Cannot set var \"{varName}\" on filters");
        if (!GetFields(filter.GetType()).TryGetValue(varName, out var fieldInfo))
            throw new DMException($"Cannot set undefined var \"{varName}\" on {filter.FilterType} filter");

        var converted = Convert(value, fieldInfo.Field.FieldType, varName);
        if (Equals(fieldInfo.Field.GetValue(filter), converted))
            return filter;

        var newFilter = filter with { };
        fieldInfo.Field.SetValue(newFilter, converted);
        return newFilter;
    }

    /// <summary>
    /// Reads a filter var. Returns false if the filter type has no such var.
    /// </summary>
    public static bool TryGetVar(DreamFilter filter, string varName, DreamObjectTree objectTree, out DreamValue value) {
        if (!GetFields(filter.GetType()).TryGetValue(varName, out var fieldInfo)) {
            value = DreamValue.Null;
            return false;
        }

        value = fieldInfo.Field.GetValue(filter) switch {
            float f => new(f),
            short s => new(s),
            // Filters store their icon as a resource ID
            int iconId => iconId != 0 && IoCManager.Resolve<DreamResourceManager>().TryLoadResource(iconId, out var icon)
                ? new(icon)
                : DreamValue.Null,
            string { Length: > 0 } str => new(str),
            Color color => new(color.A >= 1f ? color.ToHexNoAlpha().ToLower() : color.ToHex().ToLower()),
            ColorMatrix matrix => new(CreateColorMatrixList(objectTree, matrix)),
            Matrix3x2 m => new(DreamObjectMatrix.MakeMatrix(objectTree, m.M11, m.M21, m.M31, m.M12, m.M22, m.M32)),
            _ => DreamValue.Null // Null or empty string
        };

        return true;
    }

    private static DreamList CreateColorMatrixList(DreamObjectTree objectTree, ColorMatrix matrix) {
        var list = objectTree.CreateList(20);
        foreach (float entry in matrix.GetValues())
            list.AddValue(new(entry));

        return list;
    }

    private static bool SetField(DreamFilter filter, string varName, DreamValue value) {
        var fields = GetFields(filter.GetType());
        if (!fields.TryGetValue(varName, out var fieldInfo))
            return false;

        fieldInfo.Field.SetValue(filter, Convert(value, fieldInfo.Field.FieldType, varName));
        return true;
    }

    private static object Convert(DreamValue value, Type fieldType, string varName) {
        if (fieldType == typeof(float)) {
            if (value.TryGetValueAsFloat(out var floatValue))
                return floatValue;

            throw new Exception($"Value {value} is not a float");
        }

        if (fieldType == typeof(int)) {
            if (varName == "icon") {
                var resourceManager = IoCManager.Resolve<DreamResourceManager>();
                if (!resourceManager.TryLoadIcon(value, out var icon))
                    throw new Exception($"Value {value} is not a valid IconResource type");

                return icon.Id;
            }

            if (value.TryGetValueAsInteger(out var intValue))
                return intValue;

            throw new Exception($"Value {value} is not an integer");
        }

        if (fieldType == typeof(short)) {
            if (value.TryGetValueAsInteger(out var intValue))
                return (short)intValue;

            throw new Exception($"Value {value} is not an integer");
        }

        if (fieldType == typeof(string)) {
            if (value.TryGetValueAsString(out var stringValue))
                return stringValue;

            throw new Exception($"Value {value} is not a string");
        }

        if (fieldType == typeof(Color)) {
            if (value.TryGetValueAsString(out var colorString) && ColorHelpers.TryParseColor(colorString, out var color))
                return color;

            throw new Exception($"Value {value} is not a color");
        }

        if (fieldType == typeof(ColorMatrix)) {
            if (value.TryGetValueAsString(out var maybeColorString)) {
                if (ColorHelpers.TryParseColor(maybeColorString, out Color basicColor))
                    return new ColorMatrix(basicColor);
            } else if (value.TryGetValueAsDreamList(out var matrixList)) {
                if (DreamProcNativeHelpers.TryParseColorMatrix(matrixList, out ColorMatrix matrix))
                    return matrix;
            }

            throw new Exception($"Value {value} is not a color matrix");
        }

        if (fieldType == typeof(Matrix3x2)) {
            if (value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixObject)) {
                return new Matrix3x2(
                    matrixObject.A, matrixObject.D,
                    matrixObject.B, matrixObject.E,
                    matrixObject.C, matrixObject.F);
            }

            throw new Exception($"Value {value} is not a matrix");
        }

        throw new Exception($"Unsupported filter field type {fieldType}");
    }
}
