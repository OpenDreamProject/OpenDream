/proc/RunTest()
	var/database/db = new("blob.db")
	var/database/query/query = new("CREATE TABLE test (value)")
	query.Execute(db)

	var/icon/icon = new
	query.Add("INSERT INTO test VALUES (?)", icon)
	query.Execute(db)
	ASSERT(query.RowsAffected() == 1)

	query.Add("SELECT typeof(value) FROM test")
	query.Execute(db)
	ASSERT(query.NextRow())
	var/list/type_row = query.GetRowData()
	ASSERT(type_row["typeof(value)"] == "blob")
	ASSERT(!query.Error())

	// BYOND stores icons as BLOBs but returns null for BLOB columns through its DM API.
	query.Add("SELECT value FROM test")
	query.Execute(db)
	ASSERT(query.NextRow())
	ASSERT(isnull(query.GetColumn(0)))
	ASSERT(!query.Error())

	del(query)
	del(db)
	fdel("blob.db")
