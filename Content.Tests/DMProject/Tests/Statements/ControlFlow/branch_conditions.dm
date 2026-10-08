// Branching on a condition should match computing its value, including which operands get evaluated
var/calls = 0

/proc/V(x)
	global.calls++
	return x

// `E ? 1 : 0` branches on E, `!!(E)` computes its value
#define CHECK(E) \
	global.calls = 0; \
	branched = (E) ? 1 : 0; \
	branch_calls = global.calls; \
	global.calls = 0; \
	valued = !!(E); \
	ASSERT(branched == valued && branch_calls == global.calls)

/proc/RunTest()
	var/branched
	var/valued
	var/branch_calls
	var/list/values = list(0, 1, null, "", "a")
	for(var/a in values)
		for(var/b in values)
			for(var/c in values)
				CHECK(V(a))
				CHECK(!V(a))
				CHECK(V(a) && V(b))
				CHECK(V(a) || V(b))
				CHECK(!(V(a) && V(b)))
				CHECK(!(V(a) || V(b)))
				CHECK(!V(a) && !V(b))
				CHECK((V(a) && V(b)) || V(c))
				CHECK(V(a) && (V(b) || V(c)))
				CHECK(!((V(a) || V(b)) && !V(c)))
				CHECK(!(V(a) && !(V(b) || !V(c))))
				CHECK(1 && V(a))
				CHECK(0 || V(a))
				CHECK(V(a) && 0)
				CHECK(!(0 && V(a)))

	var/n = 0
	while(!(n >= 3) && n < 10)
		n++
	ASSERT(n == 3)

	n = 0
	do
		n++
	while(!(n >= 3 || n < 0))
	ASSERT(n == 3)

	n = 0
	for(var/i = 0, !(i >= 4), i++)
		n++
	ASSERT(n == 4)

	n = 0
	while(1)
		if(++n >= 5)
			break
	ASSERT(n == 5)

	if(!(n == 5) || !n)
		CRASH("wrong branch")
	else
		n = 6
	ASSERT(n == 6)
