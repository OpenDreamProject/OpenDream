// NOBYOND
// this unit test crashed byond until https://www.byond.com/forum/post/2987385 and https://www.byond.com/forum/post/2987386 are fixed
/proc/RunTest()
	var/icon/I = icon('hanoi.dmi')
	ASSERT(I.GetPixel(1,1,"0",dir=new /datum()) == "#ff0000") //what if dir was weird
	ASSERT(I.GetPixel(1,1,"0",dir="south") == "#ff0000") //what if dir was dumb
	ASSERT(I.GetPixel(1,1,"invalid iconstate") == null) //iconstate that doesn't exist
	ASSERT(I.GetPixel(1,1,"0",dir=EAST) == null) //not a dir in this file
	ASSERT(I.GetPixel(1,1,"0",dir=100) == null) //nonsense dir
	ASSERT(I.GetPixel(1,1,"0",frame=-100) == "#ff0000") //frame is clipped at 1
	ASSERT(I.GetPixel(1,1,"0",frame=100) == null) //there's only 1 frame in the file
