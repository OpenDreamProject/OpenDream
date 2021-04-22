/regex
	parent_type = /datum

	var/flags
	var/list/group
	var/index
	var/match
	var/name
	var/next
	var/text

	proc/New(pattern, flags)
		if (istype(pattern, /regex))
			var/regex/Regex = pattern

			src.name = Regex.name
			src.flags = Regex.flags
		else
			src.name = pattern
			src.flags = flags

	proc/Find(haystack, Start = 1, End = 0)

proc/regex(pattern, flags)
	return new /regex(pattern, flags)