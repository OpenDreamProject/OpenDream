/proc/RunTest()
	ASSERT(cmptext("hi", "hi"))
	ASSERT(cmptext("HI", "hi"))
	ASSERT(!cmptext("not_hi", "hi"))

	ASSERT(cmptext("/(çç)*=ç/ç=¢<>¨üäé", "/(çç)*=ç/ç=¢<>¨üäé"))
	ASSERT(cmptext("/(çç)*=ç/ç=¢<>¨üäé", "/(çç)*=ç/ç=¢<>¨ÜÄé"))

	ASSERT(!cmptext("/(çç)*=ç/ç=¢<>¨üäé", "/(çç)*=ç/ç=¢<>ÜÄé"))

	ASSERT(cmptext("string", "string", "string"))
	ASSERT(!cmptext("string", "string", "spagetti"))