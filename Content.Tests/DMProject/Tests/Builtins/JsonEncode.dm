﻿
/datum/proc/foo()

/proc/RunTest()
	ASSERT(json_encode(7) == "7")
	ASSERT(json_encode("A") == "\"A\"")
	ASSERT(json_encode('JsonEncode.dm') == "\"JsonEncode.dm\"")
	ASSERT(json_encode(list(1, 3, 5, 7, 11)) == @'[1,3,5,7,11]')
	ASSERT(json_encode(list("A" = 3, "B" = 5)) == @'{"A":3,"B":5}')
	ASSERT(json_encode(matrix(1,2,3,4,5,6)) == @'[1,2,3,4,5,6]')
	ASSERT(json_encode(/datum/proc/foo) == "\"/datum/proc/foo\"")
