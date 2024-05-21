/proc/RunTest()
	var/database/db = new("database.db")

	var/database/query/query = new("CREATE TABLE test (id int)", 1, "foo", 1.5)
	query.Execute(db)

	ASSERT(!query.Error()) // no error for too many parameters

	query.Add("INSERT INTO test VALUES (?, ?, ?)", 1)
	query.Execute(db)

	ASSERT(query.Error()) // but there is an error for too few parameters

	fdel("database.db")
	
