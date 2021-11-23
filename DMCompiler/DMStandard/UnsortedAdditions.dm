
/proc/addtext(...) 
		set opendream_unimplemented = TRUE
/proc/bounds_dist(Ref, Target)
		set opendream_unimplemented = TRUE
/proc/copytext_char(T,Start=1,End=0)
		set opendream_unimplemented = TRUE
/proc/link(url)
		set opendream_unimplemented = TRUE
/proc/filter(type, parameter, ...)
		set opendream_unimplemented = TRUE
/proc/findlasttext_char(Haystack,Needle,Start=0,End=1)
		set opendream_unimplemented = TRUE
/proc/findtext_char(Haystack,Needle,Start=1,End=0)
		set opendream_unimplemented = TRUE
/proc/issaved(v)
		set opendream_unimplemented = TRUE
/proc/shell(command)
		set opendream_unimplemented = TRUE
/proc/run(File)
		set opendream_unimplemented = TRUE
/proc/ftp(File, Name)
		set opendream_unimplemented = TRUE
/proc/replacetext_char(Haystack,Needle,Replacement,Start=1,End=0)
		set opendream_unimplemented = TRUE
/proc/spantext_char(Haystack,Needles,Start=1)
		set opendream_unimplemented = TRUE
/proc/winget(player, control_id, params)
		set opendream_unimplemented = TRUE
/proc/winexists(player, control_id)
		set opendream_unimplemented = TRUE
/proc/winclone(player, window_name, clone_name)
		set opendream_unimplemented = TRUE
/proc/winshow(player, window, show=1)
		set opendream_unimplemented = TRUE
/proc/walk_rand(Ref,Lag=0,Speed=0)
		set opendream_unimplemented = TRUE

/database
	parent_type = /datum
	proc/Close()
	proc/Error()
	proc/ErrorMsg()
	New(filename)
	proc/Open(filename)

/database/query
	proc/Add(text, ...)
	proc/Clear()
	Close()
	proc/Columns(column)
	Error()
	ErrorMsg()
	proc/Execute(database) 
	proc/GetColumn(column)
	proc/GetRowData()
	New(text, ...)
	proc/NextRow()
	proc/Reset()
	proc/RowsAffected()