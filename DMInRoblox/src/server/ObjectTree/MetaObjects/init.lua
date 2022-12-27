local module = {}

module.SetMetaObjects = function(objectTree)
	objectTree.SetMetaObject("/vector3", require(script.Vector3))
	objectTree.SetMetaObject("/part", require(script.Part))
end

return module
