//COMPILE ERROR

#define DAMN(what...,the...,hockeysticks...) world << list(##what)

/proc/RunTest()
	DAMN("do","re","mi","fa","so","la","ti","do!")
