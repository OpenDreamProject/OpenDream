// Generators built from operators must still be usable for particle effects
/proc/RunTest()
	var/particles/P = new
	P.lifespan = generator("num", 10, 20) + 5
	P.fade = generator("num", 1, 2) * 2
	P.position = generator("vector", vector(1, 1), vector(2, 2)) * 2
	P.velocity = generator("circle", 1, 2) + vector(1, 1)
	P.scale = -generator("vector", vector(1, 1), vector(2, 2))
	P.drift = generator("sphere", 1, 2) * matrix(2, 0, 0, 0, 2, 0)

	var/generator/spun = generator("vector", vector(1, 0), vector(1, 0))
	spun.Turn(90)
	P.gravity = spun

	ASSERT(P.lifespan != null)
