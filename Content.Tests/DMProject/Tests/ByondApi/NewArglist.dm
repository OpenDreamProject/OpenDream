var/const/lib = "../../../byondapitest.dll"

var/glo = "not success"

obj/asdf
	var/loca = "not success"
	New(a,b,c)
		if (a==1 && b==2 && c[3]=="three")
			glo = "success"
			loca = "success"


/proc/RunTest()
	var/obj/asdf/result = call_ext(lib, "byond:byondapitest_newarglist")("/obj/asdf",
			list(1,2,list(1,2,"three")))
	ASSERT(glo == "success")
	ASSERT(result.loca == "success")
	glo = "not success"

	result = call_ext(lib, "byond:byondapitest_newarglist")(/obj/asdf,
			list(1,2,list(1,2,"three")))
	ASSERT(glo == "success")
	ASSERT(result.loca == "success")