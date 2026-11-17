/proc/RunTest()
	fdel("reexecute.db")
	var/database/db = new("reexecute.db")
	var/database/query/query = new("CREATE TABLE test (value)")
	ASSERT(query.Execute(db))

	query.Add("INSERT INTO test VALUES (1), (2)")
	ASSERT(query.Execute(db))

	query.Add("SELECT value FROM test ORDER BY value")
	ASSERT(query.Execute(db))
	ASSERT(query.NextRow())
	var/list/first_row = query.GetRowData()
	ASSERT(first_row["value"] == 1)

	// Re-executing must discard the still-open reader and restart the result set.
	ASSERT(query.Execute(db))
	ASSERT(query.NextRow())
	var/list/reexecuted_first_row = query.GetRowData()
	ASSERT(reexecuted_first_row["value"] == 1)
	ASSERT(!query.Error())

	del(query)
	del(db)
	fdel("reexecute.db")
