
// Issue #1336 
#define JSON(e) json_decode(json_encode(e))
#define TEST(v) ASSERT(JSON(v) == v)

// These two do not support nested lists at the moment
/proc/list_test(list/l)
	var/n = length(l)
	var/lj = JSON(l)
	ASSERT(length(lj) == n)

	if(!n) return
	var/c = 0
	for (var/i in 1 to n)
		c += lj[i] == l[i]
	ASSERT(c == n)

/proc/list_assoc_test(list/l)
	var/n = length(l)
	var/lj = JSON(l)
	ASSERT(length(lj) == n)

	if(!n) return
	var/kc = 0
	var/vc = 0
	for (var/i in 1 to n)
		kc += lj[i] == l[i]
		vc += lj[lj[i]] == l[l[i]]
	ASSERT(kc == n)
	ASSERT(vc == n)

/proc/RunTest()
	TEST(0.0)
	TEST(5)
	ASSERT(isnan(JSON(1.#IND)))
	TEST(-1.#INF)
	TEST(1.#INF)

	list_test(list())
	list_test(list(1))
	list_test(list(1,2,3,4,5))

	list_assoc_test(list("c" = 5))
	list_assoc_test(list("a" = 1, "b" = 2))
	list_assoc_test(list("a" = 1, "b" = 2, "c" = 5))
