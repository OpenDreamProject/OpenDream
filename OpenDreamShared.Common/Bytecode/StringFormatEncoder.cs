using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OpenDreamShared.Common.Bytecode;

/// <summary>
/// Handles how we write format data into our strings.
/// </summary>
public static class StringFormatEncoder {
    /// <summary>
    /// This is the upper byte of the 2-byte markers we use for storing formatting data within our UTF16 strings.<br/>
    /// </summary>
    /// <remarks>
    /// It is not const because (<see langword="TODO"/>) eventually it would be desirable to make it something else<br/>
    /// (even though doing so would slightly break parity)<br/>
    /// because 0xFFxx actually maps to meaningful Unicode code points under UTF16<br/>
    /// (DM uses this because it uses UTF8 and 0xFF is just an invalid character in that encoding, no biggie)<br/>
    /// See: "Halfwidth and Fullwidth Forms" on https://en.wikibooks.org/wiki/Unicode/Character_reference/F000-FFFF
    /// </remarks>
    public static ushort FormatPrefix = 0xFF00;

    /// <summary>
    /// The lower byte of the aforementioned formatting marker thingies we stuff into our UTF16 strings.<br/>
    /// To avoid clashing with the (ALREADY ASSIGNED!) 0xFFxx code point space, these values should not exceed 0x005e (94)
    /// </summary>
    /// <remarks>
    /// <see langword="DO NOT CAST TO CHAR!"/> This requires FormatPrefix to be added to it in order to be a useful formatting character!!
    /// </remarks>
    public enum FormatSuffix : ushort {
        //States that Interpolated values can have (the [] thingies)
        StringifyWithArticle = 0x0,    //[] and we include an appropriate article for the resulting value, if necessary
        StringifyNoArticle = 0x1,      //[] and we never include an article (because it's elsewhere)
        ReferenceOfValue = 0x2,        //\ref[]

        //States that macros can have
        //(these can have any arbitrary value as long as compiler/server/client all agree)
        //(Some of these values may not align with what they are internally in BYOND; too bad!!)
        UpperDefiniteArticle,     //The
        LowerDefiniteArticle,     //the
        UpperIndefiniteArticle,   //A, An, Some
        LowerIndefiniteArticle,   //a, an, some
        UpperSubjectPronoun,      //He, She, They, It
        LowerSubjectPronoun,      //he, she, they, it
        UpperPossessiveAdjective, //His, Her, Their, Its
        LowerPossessiveAdjective, //his, her, their, its
        ObjectPronoun,            //him, her, them, it
        ReflexivePronoun,         //himself, herself, themself, it
        UpperPossessivePronoun,   //His, Hers, Theirs, Its
        LowerPossessivePronoun,   //his, hers, theirs, its

        Proper,                   //String represents a proper noun
        Improper,                 //String represents an improper noun

        LowerRoman,               //i, ii, iii, iv, v
        UpperRoman,               //I, II, III, IV, V

        OrdinalIndicator,        //1st, 2nd, 3rd, 4th, ...
        PluralSuffix,            //-s suffix at the end of a plural noun

        Icon,                    //Use an atom's icon

        ColorRed,
        ColorBlue,
        ColorGreen,
        ColorBlack,
        ColorYellow,
        ColorNavy,
        ColorTeal,
        ColorCyan,
        Bold,
        Italic
    }

    /// <summary>The default stringification state of a [] within a DM string.</summary>
    public static FormatSuffix InterpolationDefault => FormatSuffix.StringifyWithArticle;

    /// <returns>The UTF16 character we should be actually storing to articulate this format marker.</returns>
    public static char Encode(FormatSuffix suffix) {
        return (char)(FormatPrefix | ((ushort)suffix));
    }

    /// <returns>true if the input character was actually a formatting codepoint. false if not.</returns>
    public static bool Decode(char c, [NotNullWhen(true)] out FormatSuffix? suffix) {
        ushort bytes = c; // this is an implicit reinterpret_cast, in C++ lingo
        suffix = null;
        if((bytes & FormatPrefix) != FormatPrefix)
            return false;
        suffix = (FormatSuffix)(bytes & 0x00FF); // 0xFFab & 0x00FF == 0x00ab
        return true;
    }

    public static bool Decode(char c) {
        ushort bytes = c;
        return (bytes & FormatPrefix) == FormatPrefix; // Could also check that the lower byte is a valid enum but... ehhhhh
    }

    /// <returns>true if argument is a marker for an interpolated value, one of them [] things. false if not.</returns>
    public static bool IsInterpolation(FormatSuffix suffix) {
        //This logic requires that all the interpolated-value enums keep separated from the others.
        //I'd write some type-engine code to catch a discrepancy in that but alas, this language is just not OOPy enough.
        return suffix <= FormatSuffix.ReferenceOfValue;
    }

    /// <returns>A new version of the string, with all formatting characters removed.</returns>
    public static string RemoveFormatting(string input) {
        StringBuilder ret = new StringBuilder(input.Length); // Trying to keep it to one malloc here
        foreach(char c in input) {
            if(!Decode(c))
                ret.Append(c);
        }

        return ret.ToString();
    }
}
