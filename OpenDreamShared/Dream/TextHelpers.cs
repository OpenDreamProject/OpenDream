using OpenDreamShared.Dream.Procs;

namespace OpenDreamShared.Dream;

public static class TextHelpers {
    public static string GetObjectDisplayName(string name, StringFormatTypes? formatType = null) {
        bool isProper;
        if (name.Length >= 2 && name[0] == 0xFF) {
            StringFormatTypes type = (StringFormatTypes) name[1];
            isProper = (type == StringFormatTypes.Proper);
            name = name.Substring(2);
        } else {
            isProper = (name.Length == 0) || char.IsUpper(name[0]);
        }

        switch (formatType) {
            case StringFormatTypes.UpperDefiniteArticle:
                return isProper ? name : $"The {name}";
            case StringFormatTypes.LowerDefiniteArticle:
                return isProper ? name : $"the {name}";
            default:
                return name;
        }
    }
}
