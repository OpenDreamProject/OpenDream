//COMPILE ERROR OD0010

#define DAMN(what...,the...,hockeysticks...) world << list(##what)

/proc/RunTest()
	DAMN("do","re","mi","fa","so","la","ti","do!")
