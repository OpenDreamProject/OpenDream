// This is the default error/warning/notice/disable setup when the user does not mandate a different file or configuration.
// If you add a new named error with a code greater than 999, please mark it here.

//1000-1999
#pragma FileAlreadyIncluded warning
#pragma MissingIncludedFile error
#pragma InvalidWarningCode warning
#pragma MisplacedDirective error
#pragma UndefineMissingDirective warning
#pragma DefinedMissingParen error
#pragma ErrorDirective error
#pragma WarningDirective warning
#pragma MiscapitalizedDirective warning

//2000-2999
#pragma SoftReservedKeyword error
#pragma DuplicateVariable warning
#pragma DuplicateProcDefinition error
#pragma PointlessParentCall warning
#pragma PointlessBuiltinCall warning
#pragma SuspiciousMatrixCall warning
#pragma FallbackBuiltinArgument warning
#pragma PointlessScopeOperator warning
#pragma MalformedRange warning
#pragma InvalidRange error
#pragma InvalidSetStatement error
#pragma InvalidOverride warning
#pragma InvalidIndexOperation warning
#pragma DanglingVarType warning
#pragma MissingInterpolatedExpression warning
#pragma AmbiguousResourcePath warning
#pragma SuspiciousSwitchCase warning
#pragma PointlessPositionalArgument warning
#pragma ProcArgumentGlobal warning // Ref BYOND issue https://www.byond.com/forum/post/2830750
// NOTE: The next few pragmas are for OpenDream's experimental type checker
// This feature is still in development, elevating these pragmas outside of local testing is discouraged
// An RFC to finalize this feature is coming soon(TM)
// BEGIN TYPEMAKER
#pragma UnsupportedTypeCheck notice
#pragma InvalidReturnType notice
#pragma InvalidVarType notice
#pragma ImplicitNullType notice
#pragma LostTypeInfo notice
// END TYPEMAKER
#pragma UnimplementedAccess warning

//3000-3999
#pragma EmptyBlock notice
#pragma EmptyProc disabled // NOTE: If you enable this in OD's default pragma config file, it will emit for OD's DMStandard. Put it in your codebase's pragma config file.
#pragma UnsafeClientAccess disabled // NOTE: Only checks for unsafe accesses like "client.foobar" and doesn't consider if the client was already null-checked earlier in the proc
#pragma AssignmentInConditional warning 
#pragma PickWeightedSyntax disabled
#pragma AmbiguousInOrder warning
