/proc/RunTest()
	.=list()
	if(1).+=1
	ASSERT(1 in .)

	if(0).+=2
	ASSERT(!(2 in .))

	if(0) .+=2
	ASSERT(!(2 in .))

	if(0). .+=2
	ASSERT(!(2 in .))

	if(1) .+=3
	ASSERT(3 in .)

	if(1). .+=4
	ASSERT(4 in .)
