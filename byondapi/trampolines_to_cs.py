import re

re_pointer = re.compile(r"\*\s*(?:const|mut)\s+(?P<type>.+)")
re_param = re.compile(r"\s*(?P<name>[#\w]+):\s*(?P<type>.+)\s*")

TYPE_MAP = {
    "bool": "byte",
    "c_char": "byte",
    "c_void": "void",
    "u4c": "uint",
    "ByondCallback": "delegate* unmanaged[Cdecl]<void*, CByondValue>"
}

def convertType(t: str | None) -> str:
    if t is None:
        return "void"

    if mapped := TYPE_MAP.get(t):
        return mapped

    if match := re_pointer.match(t):
        return convertType(match.group("type")) + "*"

    return t

def sanitizeCsName(t: str) -> str:
    if t == "r#type":
        return "type"

    return t

file = open("trampoline/src/lib.rs", "r").read()

print("//")
print("// Function stubs")
print("//")

functions = list(re.finditer(r"^\s+fn\s+(?P<name>\w+)\((?P<params>.*)\)\s*(?:->\s*(?P<ret>.+))?;$", file, re.MULTILINE))
for match in functions:
    name = match.group("name")
    return_type = convertType(match.group("ret"))
    params = match.group("params") or ""
    if params:
        param_types = []
        for param in params.split(","):
            # print(param)
            match = re_param.match(param)
            param_name = sanitizeCsName(match.group("name"))
            param_type = convertType(match.group("type"))
            param_types.append(f"{param_type} {param_name}")
        arg_types = ", ".join(param_types)
    else:
        arg_types = ""
    print(f"""
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static {return_type} {name}({arg_types}) {{
        throw new NotImplementedException();
    }}
""")

print("//")
print("// Trampoline definition")
print("//")

for match in functions:
    name = match.group("name")
    return_type = convertType(match.group("ret"))
    params = match.group("params") or ""
    if params:
        param_types = []
        for param in params.split(","):
            # print(param)
            match = re_param.match(param)
            param_types.append(convertType(match.group("type")))
        arg_types = ", ".join(param_types) + ", "
    else:
        arg_types = ""
    print(f"        public delegate* unmanaged[Cdecl]<{arg_types}{return_type}> {name};")

print("//")
print("// Trampoline fill")
print("//")

for match in functions:
    name = match.group("name")
    print(" " * 12 + f"{name} = &{name},")