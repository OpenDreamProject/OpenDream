
/proc/RunTest()
	ASSERT(cmptext("Test", "test", "tEsT"))
	ASSERT(!cmptext("foo", "bar"))
	ASSERT(cmptextEx("hello world", "hello world"))
	ASSERT(!cmptextEx("Hello World", "hello world"))
