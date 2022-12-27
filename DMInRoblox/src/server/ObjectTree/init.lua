type ObjectDef = {
	MetaObject: MetaObject,
	Procs: {Proc}, --Doesn't support procs that override themselves multiple times
	
	Create: () -> Object
}

type Object = {
	def: ObjectDef,
	
	GetVar: (name: string) -> any,
	SetVar: (name: string, value: any) -> nil,
	CallProc: (name: string, args: {any}) -> any
}

type Proc = {
	Name: string,
	Bytecode: string
}

type MetaObject = {
	OnCreate: (obj: Object, args: {any}) -> nil,
	GetVar: (obj: Object, varName: string) -> nil,
	SetVar: (obj: Object, varName: string, value: any) -> nil
}

type JsonObjectType = {
	Path: string,
	Procs: {{number}}|nil
}

local metaObjects = require(script.MetaObjects)

local module = {}

local allProcs: {Proc} = {}
local allGlobalProcs: {[string]: int} = {}
local objectTypes: {ObjectDef} = {}
local objectTypePathToId: {[string]: number} = {}

function CreateObjectDef(objectType: JsonObjectType): ObjectDef
	local procs = {}
	if (objectType.Procs ~= nil) then
		for i = 1, #objectType.Procs do
			local proc = allProcs[objectType.Procs[i][1]+1]
			
			procs[proc.Name] = proc
		end
	end
	
	local def: ObjectDef = {
		Path = objectType.Path,
		MetaObject = nil,
		Procs = procs,
		
		Create = function(def: MetaObject, args: {any}): Object
			local obj = {
				def = def,

				GetVar = function(obj: Object, name: string): any
					if (obj.def.MetaObject ~= nil) then
						return obj.def.MetaObject.GetVar(obj, name)
					end
					
					error("Vars on non-meta objects are not supported")
				end,
				SetVar = function(obj: Object, name: string, value: any): nil
					if (obj.def.MetaObject ~= nil) then
						obj.def.MetaObject.SetVar(obj, name, value)
					else
						error("Vars on non-meta objects are not supported")
					end
				end
			}
			
			if (def.MetaObject ~= nil) then
				def.MetaObject.OnCreate(obj, args)
			end
			
			return obj
		end
	}
	
	return def
end

module.Load = function(types : {JsonObjectType}, procs: {Proc}, globalProcs: {int})
	allProcs = procs
	for i = 1, #globalProcs do
		local globalProcId = globalProcs[i]
		local globalProc = allProcs[globalProcId+1]
		
		allGlobalProcs[globalProc.Name] = globalProcId
	end
	
	for i = 1, #types do
		local objectType = types[i]
		
		objectTypePathToId[objectType.Path] = i-1
		table.insert(objectTypes, CreateObjectDef(objectType))
	end
	
	metaObjects.SetMetaObjects(module)
end

module.SetMetaObject = function(path: string, meta: MetaObject): nil
	module.GetObjectType(path).MetaObject = meta
end

module.GetGlobalProc = function(name: string): Proc
	local procId = allGlobalProcs[name]
	
	return module.GetProc(procId)
end

module.GetProc = function(id: number): Proc
	local proc = allProcs[id+1]
	
	if (proc.OwningTypeId == nil) then --Root, check if this is a native proc
		if (proc.Name == "print") then
			proc = function(args)
				print(args[1])
			end
		elseif (proc.Name == "rand") then
			proc = function(args)
				return math.random(args[1], args[2])
			end
		elseif (proc.Name == "sleep") then
			proc = function(args)
				wait(args[1])
				
				return nil
			end
		elseif (proc.Name == "cos") then
			proc = function(args)
				return math.cos(args[1])
			end
		elseif (proc.Name == "sin") then
			proc = function(args)
				return math.sin(args[1])
			end
		end
	end

	return proc
end

module.GetObjectType = function(id: number|string): ObjectDef
	if (typeof(id) == "number") then
		return objectTypes[id+1]
	elseif (typeof(id) == "string") then
		return objectTypes[objectTypePathToId[id]+1]
	end
end

return module