
// Note the parentheses wrapping the defines
#define A (273.15)
#define B (112+A)
#define C (1250+A)

/proc/RunTest()
	var/output = ((A + 20) - B) / (B - C)
	ASSERT(output > 0) // This is sufficient, see order_of_ops1.dm