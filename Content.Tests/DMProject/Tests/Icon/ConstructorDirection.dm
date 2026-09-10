// ConstructorDirection.dmi has one four-direction state: SOUTH is red and WEST is yellow.
// Constructors that specify a direction must export the selected frame as the sole SOUTH direction.
/proc/RunTest()
	var/icon/source = icon('ConstructorDirection.dmi')
	var/icon/south = icon(source, dir = SOUTH)
	var/icon/west = icon(source, dir = WEST)

	ASSERT(south.GetPixel(1, 1, "", SOUTH) == "#ff0000")
	ASSERT(south.GetPixel(1, 1, "", WEST) == null)
	ASSERT(west.GetPixel(1, 1, "", SOUTH) == "#ffff00")
	ASSERT(west.GetPixel(1, 1, "", WEST) == null)
