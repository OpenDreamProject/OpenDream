/savefile
	var/cd
	var/list/dir
	var/eof
	var/name

	proc/New(filename, timeout)
	proc/Flush()
	proc/ExportText(path = cd, file)

	proc/ImportText(path = cd, source)

	proc/Lock(timeout)
		set opendream_unimplemented = TRUE

	proc/Unlock()
		set opendream_unimplemented = TRUE
