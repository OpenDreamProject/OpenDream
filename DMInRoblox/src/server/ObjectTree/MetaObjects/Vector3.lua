return {
	OnCreate = function(object, args)
		object.X = args[1]
		object.Y = args[2]
		object.Z = args[3]
	end,
	GetVar = function(object, varName)
		if (varName == "x") then
			return object.X
		elseif (varName == "y") then
			return object.Y
		elseif (varName == "z") then
			return object.Z
		end

		return nil
	end,
}