/savefile
	var/cd
	var/list/dir
	var/eof
	var/name

	proc/New(filename, timeout)

	proc/ExportText(path = cd, file)
		set opendream_unimplemented = TRUE
		CRASH("/savefile.ExportText() is unimplemented")

	proc/Flush()

	proc/ImportText(path = cd, source)
		set opendream_unimplemented = TRUE
		CRASH("/savefile.ImportText() is unimplemented")

	proc/Lock(timeout)
		set opendream_unimplemented = TRUE
		CRASH("/savefile.Lock() is unimplemented")
		
	proc/Unlock()
		set opendream_unimplemented = TRUE
		CRASH("/savefile.Unlock() is unimplemented")