/proc/RunTest()
	var/database/db = new("clear.db")
	
	var/database/query/query = new("invalid command text")
	query.Clear()
	
	// Add with no parameters does nothing
	query.Add()
	
	// Execute without a command does nothing
	query.Execute(db)
	
	// and shouldn't report an error
	ASSERT(!query.Error())
	
	del(query)
	del(db)
	
	fdel("clear.db")