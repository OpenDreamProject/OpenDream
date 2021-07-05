/savefile
	var/cd
	var/list/dir
	var/eof
	var/name

	proc/New(filename, timeout)

	proc/ExportText(path = cd, file)
		CRASH("/savefile.ExportText() is unimplemented")

	proc/Flush()

	proc/ImportText(path = cd, source)
		CRASH("/savefile.ImportText() is unimplemented")

	proc/Lock(timeout)
		CRASH("/savefile.Lock() is unimplemented")
		
	proc/Unlock()
		CRASH("/savefile.Unlock() is unimplemented")