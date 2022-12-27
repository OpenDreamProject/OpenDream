#include "Standard/Standard.dm"

// An example that creates a wave shape with a given size
/proc/main(var/sizeX, var/sizeZ)
	var/x = sizeX
	while (x)
		x = x + -1

		var/z = sizeZ
		while (z)
			z = z + -1

			var/part/P = new()
			P.position = new /vector3(x, (cos(x*0.1)+sin(z*0.1))*10, z)
			P.size = new /vector3(1, 1, 1)

	return 1
