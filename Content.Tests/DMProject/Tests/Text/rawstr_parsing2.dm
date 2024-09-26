var/static/test_string1=@{"
foo"}
var/static/test_string2=@{"
foo
bar"}
var/static/test_string3=@{"
foo
bar
"}
var/static/test_string4=@{"

foo
bar

"}

/proc/RunTest()
	ASSERT(length(test_string1) == 3)
	ASSERT(length(test_string2) == 7)
	ASSERT(length(test_string3) == 7)
	ASSERT(length(test_string4) == 9)