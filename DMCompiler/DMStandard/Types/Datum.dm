/datum
	var/tmp/type as path(/datum)|opendream_compiletimereadonly
	var/tmp/parent_type

	var/tmp/list/vars as opendream_compiletimereadonly

	var/tag

	proc/New()
	//SAFETY: If you redefine this to anything except empty, please revisit how DreamObject handles Del() or it will
	//        attempt to run DM on a GC thread, potentially causing problems.
	proc/Del()

	proc/Topic(href, href_list)

	proc/Read(savefile/F)

	proc/Write(savefile/F)
