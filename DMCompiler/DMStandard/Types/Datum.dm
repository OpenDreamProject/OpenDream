/datum
	var/type
	var/parent_type

	var/list/vars

	var/tag

	proc/New()

	proc/Del()
		if(tag)
			tag = null

	proc/Topic(href, href_list)
