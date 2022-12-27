type VM = {
	strings: {string},
	bytecode: {number},
	pc: number,
	stack: {any},
	locals: {any},
	argCount: number,

	Push: (VM, any) -> nil,
	Pop: (VM) -> any,
	ReadByte: (VM) -> number,
	ReadInt: (VM) -> number,
	ReadFloat: (VM) -> number,
	ReadString: (VM) -> string,
	ReadReference: (VM) -> DMReference,
	GetReferenceValue: (VM, DMReference) -> any,
	AssignReference: (VM, DMReference, any) -> nil
}

type DMReference = {
	Type: "GlobalProc"|"Field"|"Local"|"Argument",
	Id: number|string
}

local base64 = require(script.Base64)
local objectTree = require(script.Parent.ObjectTree)

function isTruthy(value)
	local valueType = type(value)

	if (valueType == "number") then
		return value ~= 0
	end

	error("No isTruthy for " ..value .. " (" ..valueType.. ")")
end

local module = {}

module.strings = {}

module.Load = function(strings: {string})
	module.strings = strings
end

module.RunBytecode = function(bytecodeBase64: string, args: {any})
	local returnValue = 2
	local vm:VM = {
		bytecode = base64.decode(bytecodeBase64),
		pc = 1,
		stack = {},
		locals = {},
		argCount = #args,

		Push = function(vm: VM, value: any): nil
			table.insert(vm.stack, value)
		end,
		Pop = function(vm: VM): any
			return table.remove(vm.stack)
		end,
		ReadByte = function(vm: VM): number
			vm.pc += 1
			return vm.bytecode[vm.pc - 1]
		end,
		ReadInt = function(vm: VM): number
			local x = 0
			for i = 0, 3 do
				x = bit32.bor(x, bit32.lshift(vm.bytecode[vm.pc + i], i * 8))
			end
			
			vm.pc += 4
			return x
		end,
		ReadFloat = function(vm: VM): number
			local x = {}
			for i = 0, 3 do
				x[i + 1] = vm.bytecode[vm.pc + i]
			end
			vm.pc += 4
			
			local sign = 1
			local mantissa = x[3] % 128

			for i = 2, 1, -1 do mantissa = mantissa * 256 + x[i] end

			if x[4] > 127 then sign = -1 end
			local exponent = (x[4] % 128) * 2 +
				math.floor(x[3] / 128)
			if exponent == 0 then return 0 end
			mantissa = (math.ldexp(mantissa, -23) + 1) * sign
			return math.ldexp(mantissa, exponent - 127)
		end,
		ReadString = function(vm: VM): string
			return module.strings[vm:ReadInt() + 1]
		end,
		ReadReference = function(vm: VM): DMReference
			local refType: number = vm:ReadByte()
			local reference: DMReference = {}
			
			if (refType == 5) then
				reference.Type = "Argument"
				reference.Id = vm:ReadByte()
			elseif (refType == 6) then
				reference.Type = "Local"
				reference.Id = vm:ReadByte()
			elseif (refType == 8) then
				reference.Type = "Field"
				reference.Id = vm:ReadString()
			elseif (refType == 11) then
				reference.Type = "GlobalProc"
				reference.Id = vm:ReadInt()
			else
				error("Unknown reference type " ..refType)
			end
		
			return reference
		end,
		GetReferenceValue = function(vm: VM, ref: DMReference)
			if (ref.Type == "Argument") then
				return vm.locals[ref.Id]
			elseif (ref.Type == "Local") then
				return vm.locals[ref.Id + vm.argCount]
			elseif (ref.Type == "Field") then
				local owner = vm:Pop()
				
				return owner:GetVar(ref.Id)
			else
				error("Unknown reference type \"" ..ref.Type.. "\"")
			end
		end,
		AssignReference = function(vm: VM, ref: DMReference, value: any)
			if (ref.Type == "Argument") then
				vm.locals[ref.Id] = value
			elseif (ref.Type == "Local") then
				vm.locals[ref.Id + vm.argCount] = value
			elseif (ref.Type == "Field") then
				local owner = vm:Pop()
				
				owner:SetVar(ref.Id, value)
			else
				error("Unknown reference type \"" ..ref.Type.. "\"")
			end
		end,
	}
	
	for argIndex = 1, #args do
		vm.locals[argIndex - 1] = args[argIndex]
	end
	
	while (vm.pc <= #vm.bytecode) do
		local opcode = vm.bytecode[vm.pc]
		
		vm.pc += 1
		if (opcode == 0x2) then --PushType
			local objectTypeId = vm:ReadInt()
			
			vm:Push(objectTree.GetObjectType(objectTypeId))
		elseif (opcode == 0x6) then --PushReferenceValue
			local reference = vm:ReadReference()
			
			vm:Push(vm:GetReferenceValue(reference))
		elseif (opcode == 0x8) then --Add
			local b = vm:Pop()
			local a = vm:Pop()
			
			local aType = typeof(a)
			local bType = typeof(b)
			if (aType == "number" and bType == "number") then
				vm:Push(a + b)
			else
				error("Cannot add " ..aType.. " and " ..bType)
			end
		elseif (opcode == 0x9) then --Assign
			local reference = vm:ReadReference()
			local value = vm:Pop()
			
			vm:AssignReference(reference, value)
		elseif (opcode == 0xA) then --Call
			local procRef = vm:ReadReference()
			local args = vm:Pop()
			
			local proc
			if (procRef.Type == "GlobalProc") then
				proc = objectTree.GetProc(procRef.Id)
			else
				error("Unknown proc reference type " .. procRef.Type)
			end
			
			if (typeof(proc) == "function") then --Native proc
				vm:Push(proc(args))
			elseif (typeof(proc) == "table") then --DM proc
				vm:Push(module.RunBytecode(proc.Bytecode, args))
			end
		elseif (opcode == 0xC) then --JumpIfFalse
			local pos = vm:ReadInt()
			local value = vm:Pop()
			
			if (not isTruthy(value)) then
				vm.pc = pos + 1
			end
		elseif (opcode == 0xE) then --Jump
			vm.pc = vm:ReadInt() + 1
		elseif (opcode == 0x10) then --Return
			returnValue = vm:Pop()
			break
		elseif (opcode == 0x28) then --Multiply
			local a = vm:Pop()
			local b = vm:Pop()
			
			local aType = typeof(a)
			local bType = typeof(b)
			if (aType == "number" and bType == "number") then
				vm:Push(a * b)
			else
				error("Cannot multiply " ..aType.. " and " ..bType)
			end
		elseif (opcode == 0x2E) then --CreateObject
			local args = vm:Pop()
			local objectType = vm:Pop()
			
			
			vm:Push(objectType:Create(args))
		elseif (opcode == 0x37) then --PushArguments
			local argCount = vm:ReadInt()
			local namedCount = vm:ReadInt()
			
			assert(namedCount == 0, "Named arguments aren't supported")
			
			vm.pc += argCount
			
			local args = {}
			for i = 0, argCount-1 do
				args[argCount - i] = vm:Pop()
			end
			
			vm:Push(args)
		elseif (opcode == 0x38) then --PushFloat
			vm:Push(vm:ReadFloat())
		elseif (opcode == 0x43) then --DebugSource
			vm:ReadString()
		elseif (opcode == 0x44) then --DebugLine
			vm:ReadInt()
		elseif (opcode == 0x51) then --Pop
			vm:Pop()
		else
			error("Unknown opcode " ..opcode)
		end
	end
	
	return returnValue
end

return module