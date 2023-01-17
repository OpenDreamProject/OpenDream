// This is the default error/warning/notice/disable setup when the user does not mandate a different file or configuration.
// If you add a new named error with a code greater than 999, please mark it here.

//1000-1999
#pragma FileAlreadyIncluded warning
#pragma MissingIncludedFile error
#pragma MisplacedDirective error
#pragma UndefineMissingDirective warning
#pragma DefinedMissingParen error
#pragma ErrorDirective error
#pragma WarningDirective warning
#pragma MiscapitalizedDirective warning

//2000-2999
#pragma SoftReservedKeyword error
#pragma DuplicateVariable error
#pragma DuplicateProcDefinition error
#pragma TooManyArguments error
#pragma PointlessParentCall warning
#pragma PointlessBuiltinCall warning
#pragma MalformedRange warning
#pragma InvalidRange error
#pragma InvalidSetStatement error
#pragma DanglingVarType warning

//3000-3999
#pragma EmptyBlock warning
