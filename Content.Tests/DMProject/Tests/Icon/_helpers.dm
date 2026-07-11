// IGNORE

/proc/CompareIcons(icon/generated, icon/expected)
	for(var/y in 1 to expected.Height())
		for(var/x in 1 to expected.Width())
			if(generated.GetPixel(x, y) != expected.GetPixel(x, y))
				world.log << "[x] [y]"
				world.log << generated.GetPixel(x, y)
				world.log << expected.GetPixel(x, y)
				return FALSE
	return TRUE