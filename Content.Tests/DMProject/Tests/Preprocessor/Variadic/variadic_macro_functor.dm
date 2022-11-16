
#define SEND_SIGNAL(target, arguments...) list(target, ##arguments)

/proc/RunTest()
	var/list/a = SEND_SIGNAL(2)
	ASSERT(a ~= list(2,))
	ASSERT(a ~= list(2))
	var/list/b = SEND_SIGNAL(2,3,4,5)
	ASSERT(b ~= list(2,3,4,5))
