using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace OpenDreamShared.Dream.Procs {
    public enum DreamProcOpcode {
        BitShiftLeft = 0x1,
        PushType = 0x2,
        PushString = 0x3,
        FormatString = 0x4,
        SwitchCaseRange = 0x5,
        PushReferenceValue = 0x6,
        PushPath = 0x7,
        Add = 0x8,
        Assign = 0x9,
        Call = 0xA,
        MultiplyReference = 0xB,
        JumpIfFalse = 0xC,
        JumpIfTrue = 0xD,
        Jump = 0xE,
        CompareEquals = 0xF,
        Return = 0x10,
        PushNull = 0x11,
        Subtract = 0x12,
        CompareLessThan = 0x13,
        CompareGreaterThan = 0x14,
        BooleanAnd = 0x15,
        BooleanNot = 0x16,
        DivideReference = 0x17,
        Negate = 0x18,
        Modulus = 0x19,
        Append = 0x1A,
        CreateRangeEnumerator = 0x1B,
        //0x1C
        CompareLessThanOrEqual = 0x1D,
        CreateAssociativeList = 0x1E,
        Remove = 0x1F,
        DeleteObject = 0x20,
        PushResource = 0x21,
        CreateList = 0x22,
        CallStatement = 0x23,
        BitAnd = 0x24,
        CompareNotEquals = 0x25,
        //0x26
        Divide = 0x27,
        Multiply = 0x28,
        BitXorReference = 0x29,
        BitXor = 0x2A,
        BitOr = 0x2B,
        BitNot = 0x2C,
        Combine = 0x2D,
        CreateObject = 0x2E,
        BooleanOr = 0x2F,
        PushArgumentList = 0x30,
        CompareGreaterThanOrEqual = 0x31,
        SwitchCase = 0x32,
        Mask = 0x33,
        //0x34
        Error = 0x35,
        IsInList = 0x36,
        PushArguments = 0x37,
        PushFloat = 0x38,
        ModulusReference = 0x39,
        CreateListEnumerator = 0x3A,
        Enumerate = 0x3B,
        DestroyEnumerator = 0x3C,
        Browse = 0x3D,
        BrowseResource = 0x3E,
        OutputControl = 0x3F,
        BitShiftRight = 0x40,
        //0x41
        Power = 0x42,
        //0x43
        //0x44
        Prompt = 0x45,
        PushProcArguments = 0x46,
        Initial = 0x47,
        //0x48
        IsType = 0x49,
        LocateCoord = 0x4A,
        Locate = 0x4B,
        IsNull = 0x4C,
        Spawn = 0x4D,
        //0x4E
        //0x4F,
        JumpIfNullDereference = 0x50,
        Pop = 0x51,
        Prob = 0x52,
        IsSaved = 0x53,
        PickUnweighted = 0x54,
        PickWeighted = 0x55,
        Increment = 0x56,
        Decrement = 0x57,
        CompareEquivalent = 0x58,
        CompareNotEquivalent = 0x59,
        Throw = 0x5A,
        IsInRange = 0x5B,
        MassConcatenation = 0x5C,
        CreateTypeEnumerator = 0x5D,
        //0x5E
        PushGlobalVars = 0x5F
    }

    public enum DreamProcOpcodeParameterType {
        Named = 0xFC,
        Unnamed = 0xFD
    }

    /// <summary>
    /// Handles how we write format data into our strings.
    /// </summary>
    public static class StringFormatEncoder
    {
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
        public static UInt16 FormatPrefix = 0xFF00;

        /// <summary>
        /// The lower byte of the aforementioned formatting marker thingies we stuff into our UTF16 strings.<br/>
        /// To avoid clashing with the (ALREADY ASSIGNED!) 0xFFxx code point space, these values should not exceed 0x005e (94)
        /// </summary>
        /// <remarks>
        /// <see langword="DO NOT CAST TO CHAR!"/> This requires FormatPrefix to be added to it in order to be a useful formatting character!!
        /// </remarks>
        public enum FormatSuffix : UInt16
        {
            //States that Interpolated values can have (the [] thingies)
            StringifyWithArticle = 0x0,    //[] and we include an appropriate article for the resulting value, if necessary            
            StringifyNoArticle = 0x1,      //[] and we never include an article (because it's elsewhere)
            ReferenceOfValue = 0x2,        //\ref[]

            //States that macros can have
            //(these can have any arbitrary value as long as compiler/server/cilent all agree)
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
        public static char Encode(FormatSuffix suffix)
        {
            return (char)(FormatPrefix | ((UInt16)suffix));
        }

        /// <returns>true if the input character was actually a formatting codepoint. false if not.</returns>
        public static bool Decode(char c, [NotNullWhen(true)] out FormatSuffix? suffix)
        {
            UInt16 bytes = c; // this is an implicit reinterpret_cast, in C++ lingo
            suffix = null;
            if((bytes & FormatPrefix) != FormatPrefix)
                return false;
            suffix = (FormatSuffix)(bytes & 0x00FF); // 0xFFab & 0x00FF == 0x00ab
            return true;
        }
        public static bool Decode(char c)
        {
            UInt16 bytes = c;
            return (bytes & FormatPrefix) == FormatPrefix; // Could also check that the lower byte is a valid enum but... ehhhhh
        }

        /// <returns>true if argument is a marker for an interpolated value, one of them [] things. false if not.</returns>
        public static bool IsInterpolation(FormatSuffix suffix)
        {
            //This logic requires that all the interpolated-value enums keep separated from the others.
            //I'd write some type-engine code to catch a discrepancy in that but alas, this language is just not OOPy enough.
            return suffix <= FormatSuffix.ReferenceOfValue;
        }

        /// <returns>A new verison of the string, with all formatting characters removed.</returns>
        public static string RemoveFormatting(string input)
        {
            StringBuilder ret = new StringBuilder(input.Length); // Trying to keep it to one malloc here
            foreach(char c in input)
            {
                if(!Decode(c))
                    ret.Append(c);
            }
            return ret.ToString();
        }
    }

    ///<summary>
    ///Stores any explicit casting done via the "as" keyword. Also stores compiler hints for DMStandard.<br/>
    ///is a [Flags] enum because it's possible for something to have multiple values (especially with the quirky DMStandard ones)
    /// </summary>
    [Flags]
    public enum DMValueType {
        Anything = 0x0,
        Null = 0x1,
        Text = 0x2,
        Obj = 0x4,
        Mob = 0x8,
        Turf = 0x10,
        Num = 0x20,
        Message = 0x40,
        Area = 0x80,
        Color = 0x100,
        File = 0x200,
        CommandText = 0x400,
        Sound = 0x800,
        Icon = 0x1000,
        //Byond here be dragons
        Unimplemented = 0x2000, // Marks that a method or property is not implemented. Throws a compiler warning if accessed.
        CompiletimeReadonly = 0x4000, // Marks that a property can only ever be read from, never written to. This is a const-ier version of const, for certain standard values like list.type
    }

    public struct DMReference {
        public static readonly DMReference Src = new() { RefType = Type.Src };
        public static readonly DMReference Self = new() { RefType = Type.Self };
        public static readonly DMReference Usr = new() { RefType = Type.Usr };
        public static readonly DMReference Args = new() { RefType = Type.Args };
        public static readonly DMReference SuperProc = new() { RefType = Type.SuperProc };
        public static readonly DMReference ListIndex = new() { RefType = Type.ListIndex };

        public enum Type : byte {
            Src,
            Self,
            Usr,
            Args,
            ListIndex,
            Argument,
            Local,
            Global,
            Field,
            SrcField,
            Proc,
            GlobalProc,
            SrcProc,
            SuperProc
        }

        public Type RefType;

        //Argument, Local, Global, GlobalProc
        public int Index;

        //Field, SrcField, Proc, SrcProc
        public string Name;

        public static DMReference CreateArgument(int argId) {
            if (argId > 255) throw new Exception("Argument id is greater than the maximum of 255");

            return new DMReference() { RefType = Type.Argument, Index = (byte)argId };
        }

        public static DMReference CreateLocal(int local) {
            if (local > 255) throw new Exception("Local variable id is greater than the maximum of 255");

            return new DMReference() { RefType = Type.Local, Index = (byte)local };
        }

        public static DMReference CreateGlobal(int global) {
            return new DMReference() { RefType = Type.Global, Index = global };
        }

        public static DMReference CreateField(string fieldName) {
            return new DMReference() { RefType = Type.Field, Name = fieldName };
        }

        public static DMReference CreateSrcField(string fieldName) {
            return new DMReference() { RefType = Type.SrcField, Name = fieldName };
        }

        public static DMReference CreateProc(string procName) {
            return new DMReference() { RefType = Type.Proc, Name = procName };
        }

        public static DMReference CreateGlobalProc(int procId) {
            return new DMReference() { RefType = Type.GlobalProc, Index = procId };
        }

        public static DMReference CreateSrcProc(string procName) {
            return new DMReference() { RefType = Type.SrcProc, Name = procName };
        }

        public override string ToString() {
            switch (RefType) {
                case Type.Local:
                case Type.Global:
                case Type.Argument:
                case Type.GlobalProc:
                    return $"{RefType} {Index}";

                case Type.SrcField:
                case Type.Field:
                case Type.SrcProc:
                case Type.Proc:
                    return $"{RefType} \"{Name}\"";

                default: return RefType.ToString();
            }
        }
    }

    // Dummy class-as-namespace because C# just kinda be like this
    public static class OpcodeVerifier
    {
        /// <summary>
        /// Validates that the opcodes in DreamProcOpcode are all unique, such that none resolve to the same byte.
        /// </summary>
        /// <returns>True if there are duplicate opcodes, false if not</returns>
        public static bool AreOpcodesInvalid() // FIXME: Can this be made into something done during compiletime? Like, *this code's* compiletime? >:/
        {
            // I'm not *too* satisfied with this boolean schtick, as opposed to throwing,
            // but since we're in OpenDreamShared I want each executable to be able to do what they want with this information.

            // Key is an int (or whatever the underlying type is) we're already using for an opcode
            HashSet<DreamProcOpcode> bytePool = new();
            foreach (DreamProcOpcode usedInt in Enum.GetValues(typeof(DreamProcOpcode)))
            {
                if(bytePool.Contains(usedInt))
                {
                    return true;
                }
                bytePool.Add(usedInt);
            }
            return false;
        }
    }
}
