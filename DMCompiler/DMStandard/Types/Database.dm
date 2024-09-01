/database
	parent_type = /datum
	proc/Close()
	proc/Error()
	proc/ErrorMsg()
	New(filename)
	proc/Open(filename)

/database/query
	var/_binobj as opendream_unimplemented
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
