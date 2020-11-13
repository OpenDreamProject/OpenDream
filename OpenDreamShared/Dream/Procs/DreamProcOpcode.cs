namespace OpenDreamShared.Dream.Procs {
    enum DreamProcOpcode {
        BitShiftLeft = 0x1,
        GetIdentifier = 0x2,
        PushString = 0x3,
        BuildString = 0x4,
        PushInt = 0x5,
        DefineVariable = 0x6,
        PushPath = 0x7,
        Add = 0x8,
        Assign = 0x9,
        Call = 0xA,
        Dereference = 0xB,
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
        PushSuperProc = 0x17,
        Negate = 0x18,
        Modulus = 0x19,
        Append = 0x1A,
        CreateScope = 0x1B,
        DestroyScope = 0x1C,
        CompareLessThanOrEqual = 0x1D,
        IndexList = 0x1E,
        Remove = 0x1F,
        DeleteObject = 0x20,
        PushResource = 0x21,
        //0x22
        CallStatement = 0x23,
        BitAnd = 0x24,
        CompareNotEquals = 0x25,
        //0x26
        Divide = 0x27,
        Multiply = 0x28,
        PushSelf = 0x29,
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
        //0x35
        IsInList = 0x36,
        PushArguments = 0x37,
        PushDouble = 0x38,
        PushSrc = 0x39,
        CreateListEnumerator = 0x3A,
        EnumerateList = 0x3B,
        DestroyListEnumerator = 0x3C,

        BooleanExpressionEnd = 0xF7,

        BuildStringPartString = 0xF8,
        BuildStringPartExpression = 0xF9,
        BuildStringPartExpressionEnd = 0xFA,
        BuildStringEnd = 0xFB
    }

    enum DreamProcOpcodeParameterType {
        Named = 0xFC,
        Unnamed = 0xFD
    }
}
