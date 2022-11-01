// We do HSV gradients our own way

/proc/RunTest()
	var/list/gradlist = list(0, "#ff0000", 1, "#00ff00")
	ASSERT(gradient("#ff0000", "#00ff00", 0.5) == "#7f7f00")
	ASSERT(gradient(gradlist, 0.5) == "#7f7f00")
	ASSERT(gradient(gradlist, 0) == "#ff0000")
	ASSERT(gradient(gradlist, 1) == "#00ff00")
	ASSERT(gradient(gradlist, "loop", 1.5) == "#7f7f00")
	ASSERT(gradient(gradlist, "loop", 1.1) == "#e51900")
	ASSERT(gradient(gradlist, "loop", 2) == "#ff0000")
	ASSERT(gradient(gradlist, "loop", 1) == "#ff0000")