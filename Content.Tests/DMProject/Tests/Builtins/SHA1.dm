/proc/RunTest()
	ASSERT(sha1("ABCDEF") == "970093678b182127f60bb51b8af2c94d539eca3a")
	ASSERT(sha1("abcdef") == "1f8ac10f23c5b5bc1167bda84b833e5c057a77d2")
	ASSERT(sha1(file('turf.dmi')) == "513e55e301c44636c3e9cf6162cf896107c1d984")