local vm = require(script.VM)
local objectTree = require(script.ObjectTree)
local json = require(script.DM)

vm.Load(json.Strings)
objectTree.Load(json.Types, json.Procs, json.GlobalProcs)

local mainProc = objectTree.GetGlobalProc("main")
local args = {100, 50}
local returned = vm.RunBytecode(mainProc.Bytecode, args)
print("Returned", returned)