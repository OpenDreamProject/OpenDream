/datum
	var/const/type
	var/tmp/parent_type

	var/const/list/vars

	var/tag

	proc/New()
	//SAFETY: If you redefine this to anything except empty, please revisit how DreamObject handles Del() or it will
	//        attempt to run DM on a GC thread, potentially causing problems.
	proc/Del()

	proc/Topic(href, href_list)

	proc/Read(savefile/F)

	proc/Write(savefile/F)
