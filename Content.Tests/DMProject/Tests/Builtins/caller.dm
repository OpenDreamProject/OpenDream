// Tests caller by propagating a value between caller procs
#define COMPARE(a, b) if(a != b) {CRASH("Assertion failed: expected [b] got [a]")}
#define OLD_VALUE 32
#define NEW_VALUE 50

/root/New()
	COMPARE(caller.name, nameof(/proc/RunTest))

/root/proc/test_propagation(val = OLD_VALUE)
	COMPARE(val, OLD_VALUE)
	// we override the val argument's value with a new value across all subtypes
	var/callee/caller_chain = callee
	do
		caller_chain.args[1] = NEW_VALUE
		caller_chain = caller_chain.caller
	while(caller_chain.name == callee.name)
	COMPARE(val, NEW_VALUE)

/root/layer1/test_propagation(val = OLD_VALUE)
	COMPARE(val, OLD_VALUE)
	. = ..() // propagate
	COMPARE(val, NEW_VALUE)

/root/layer1/layer2/test_propagation(val = OLD_VALUE)
	COMPARE(val, OLD_VALUE)
	. = ..() // propagate
	COMPARE(val, NEW_VALUE)

/proc/RunTest(val = OLD_VALUE)
	var/root/test_datum = new /root/layer1/layer2()
	test_datum.test_propagation()
	COMPARE(val, OLD_VALUE) // we shouldn't reach this
