
// In some math, null is coerced to 0.
// Lets make sure that works in a const context.

/proc/RunTest()
	var/const/x = null & null | null ^ null
	ASSERT(x == 0)
	var/const/y = null + null - null * null
	ASSERT(y == 0)
	var/const/z = null / 1
	ASSERT(z == 0)
	var/const/alpha = null << null >> null
	ASSERT(alpha == 0)
	var/const/txt = null + "seven" + null
	ASSERT(txt == "seven")