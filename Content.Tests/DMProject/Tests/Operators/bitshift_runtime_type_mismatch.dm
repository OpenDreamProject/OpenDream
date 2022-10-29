//RUNTIME ERROR

/proc/RunTest()
	//Making sure it doesn't work in non-const contexts
	var/x = 5
	x = 2
	var/y = "foo"
	y = "bar"
	ASSERT((x << y) == 2)
	ASSERT((y << x) == 0)