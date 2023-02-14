//For some reason (probably some ancient Dancode), there can be some trash after certain block-initiating expressions.
//I don't think any of our targets use this, but it did show up in OpenSS13.

/proc/RunTest()
	//Quirky if statements
	if(1).
		ASSERT(TRUE)
	else
		ASSERT(FALSE)
	if(2):
		ASSERT(TRUE)
	else
		ASSERT(FALSE)

	//Quirky while
	var/x = FALSE
	while(TRUE).
		x = TRUE
		break
	ASSERT(x)
	//Semicolons, too, apparently
	var/i = 3
	while(i);
	 ASSERT(i)
	 i -= 1
	ASSERT(i == 0)