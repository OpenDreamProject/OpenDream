/regex
	parent_type = /datum

	var/flags
	var/list/group
	var/index
	var/match
	var/name
	var/next
	var/text

	New(pattern, flags)
		if (istype(pattern, /regex))
			var/regex/Regex = pattern

			src.name = Regex.name
			src.flags = Regex.flags
		else
			src.name = pattern
			src.flags = flags

	proc/Find(haystack, Start = 1, End = 0)

	proc/Find_char(haystack, Start = 1, End = 0)
		set opendream_unimplemented = TRUE

	proc/Replace(haystack, replacement, Start = 1, End = 0)

	proc/Replace_char(haystack, replacement, Start = 1, End = 0)
		set opendream_unimplemented = TRUE
		return haystack

proc/regex(pattern, flags)
