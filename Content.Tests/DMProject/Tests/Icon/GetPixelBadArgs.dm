/proc/RunTest()
	var/icon/I = icon('hanoi.dmi')
	ASSERT(I.GetPixel(1,1,"invalid iconstate") == null) //iconstate that doesn't exist
	ASSERT(I.GetPixel(1,1,"0",dir=EAST) == null) //not a dir in this file
	ASSERT(I.GetPixel(1,1,"0",dir=100) == null) //nonsense dir
	ASSERT(I.GetPixel(1,1,"0",frame=-100) == "#ff0000") //frame is clipped at 1
	ASSERT(I.GetPixel(1,1,"0",frame=100) == null) //there's only 1 frame in the file
