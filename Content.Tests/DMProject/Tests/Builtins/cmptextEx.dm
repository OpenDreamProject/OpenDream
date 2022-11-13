/proc/RunTest()
	ASSERT(cmptextEx("hi", "hi"))
	ASSERT(!cmptextEx("HI", "hi"))
	ASSERT(!cmptextEx("not_hi", "hi"))

	ASSERT(cmptextEx("/(çç)*=ç/ç=¢<>¨üäé", "/(çç)*=ç/ç=¢<>¨üäé"))
	ASSERT(!cmptextEx("/(çç)*=ç/ç=¢<>¨üäé", "/(çç)*=ç/ç=¢<>¨ÜÄé"))

	ASSERT(!cmptextEx("/(çç)*=ç/ç=¢<>¨üäé", "/(çç)*=ç/ç=¢<>ÜÄé"))

	ASSERT(cmptextEx("string", "string", "string"))
	ASSERT(!cmptextEx("string", "string", "spagetti"))