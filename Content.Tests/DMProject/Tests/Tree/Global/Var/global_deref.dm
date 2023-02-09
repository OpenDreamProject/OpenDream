
var/const/a = "a"
var/static/b = "b"

/proc/fn()
	return "fn"

/datum/proc/fn()
	return "datumfn"

/datum/proc/main()
	var/a = "aloc"
	var/b = "bloc"

	ASSERT(a == "aloc")
	ASSERT(global.a == "a")
	ASSERT(b == "bloc")
	ASSERT(global.b == "b")
	ASSERT(fn() == "datumfn")
	ASSERT(global.fn() == "fn")
	ASSERT(abs(-1) == 1)

/proc/RunTest()
	var/datum/d = new
	d.main()
