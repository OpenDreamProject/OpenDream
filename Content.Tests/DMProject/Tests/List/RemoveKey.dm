/proc/RunTest()
	var/list/L = list("foo" = "bar")

	// Removing the key should remove the associated value as well
	L -= "foo"
	ASSERT(L["foo"] == null)
