
//# issue 104

#define A(a, all...) var/a = list(all)

/proc/fn1(a, b, ...)
	ASSERT(a == 1)
	ASSERT(b == 2)
	ASSERT(args.len == 4)

/proc/fn2(a, ..., b)
	ASSERT(a == 1)
	ASSERT(b == 2)
	ASSERT(args.len == 4)

/proc/RunTest()
	A(b, 2, 3, 4)
	fn1(1,2,3,4)
	fn2(1,2,3,4)
