// We do HSV gradients our own way

/proc/RunTest()
	var/list/gradlist = list(0, "#ff0000", 1, "#00ff00")
	var/list/gradlistloop = list(0, "#ff0000", "loop", 1, "#00ff00")

	ASSERT(gradient("#ff0000", "#00ff00", 0.5) == "#7f7f00")
	ASSERT(gradient(gradlist, 0.5) == "#7f7f00")
	ASSERT(gradient(gradlist, 0) == "#ff0000")
	ASSERT(gradient(gradlist, 1) == "#00ff00")
	ASSERT(gradient(gradlistloop, 1.5) == "#7f7f00")
	ASSERT(gradient(gradlistloop, 1.1) == "#e51900")
	ASSERT(gradient(gradlistloop, 2) == "#ff0000")
	ASSERT(gradient(gradlistloop, 1) == "#ff0000")
	ASSERT(gradient("#ff0000", "#00ff00", space=COLORSPACE_HSV, 0.5) == "#ffff00")
	ASSERT(gradient("#640000", "#006400", space=COLORSPACE_HSV, 0.005) == "#640100")
	ASSERT(gradient("#ff0000", "#000064", space=COLORSPACE_HSV, 1) == "#000064")
	ASSERT(gradient("#ff0000", "#000064", space=COLORSPACE_HSV, 0) == "#ff0000")
	ASSERT(gradient(0, "#ff0000", 0.5, "#00ff00", 1, "#0000ff", 0.96) == "#0014ea")
	ASSERT(gradient(0, "white", 0.5, new /datum(), 1, "white", 0.75) == "#7f7f7f7f")

	ASSERT(gradient(arglist(list(index = 0.5, "red", "black"))) == "#7f0000")
