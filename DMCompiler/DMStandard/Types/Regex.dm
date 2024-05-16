﻿/regex
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

	proc/Find(haystack, start = 1, end = 0)
		return findtext(text, src, start, end)

	proc/Find_char(haystack, start = 1, end = 0)
		set opendream_unimplimented = TRUE
	
	proc/Replace(haystack, replacement, start = 1, end = 0)
		return replacetext(text, src, replacement, start, end)

	proc/Replace_char(haystack, replacement, start = 1, end = 0)
		set opendream_unimplimented = TRUE

proc/regex(pattern, flags)
