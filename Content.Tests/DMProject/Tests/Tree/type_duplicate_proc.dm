//COMPILE ERROR OD2101
// NOBYOND
#pragma DuplicateProcDefinition error

//Issue OD#933: https://github.com/OpenDreamProject/OpenDream/issues/933

/datum/proc/RunTest()
	return
/atom/proc/RunTest()
	return
/obj/proc/RunTest()
	return
/proc/RunTest()
	return
