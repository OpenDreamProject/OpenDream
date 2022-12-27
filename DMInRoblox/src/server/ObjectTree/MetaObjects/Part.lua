local objectTree = require(script.Parent.Parent)

return {
	OnCreate = function(object)
		object.Part = Instance.new("Part")
		object.Part.Anchored = true
		object.Part.Parent = workspace
	end,
	GetVar = function(object, varName)
		if (varName == "position") then
			return objectTree.GetObjectType("/vector3"):Create({object.Part.Position.X, object.Part.Position.Y, object.Part.Position.Z})
		end

		return nil
	end,
	SetVar = function(object, varName, value)
		if (varName == "position") then
			local vector = Vector3.new(value:GetVar("x"), value:GetVar("y"), value:GetVar("z"))

			object.Part.Position = vector
		elseif (varName == "size") then
			local vector = Vector3.new(value:GetVar("x"), value:GetVar("y"), value:GetVar("z"))
			
			object.Part.Size = vector
		end
	end
}