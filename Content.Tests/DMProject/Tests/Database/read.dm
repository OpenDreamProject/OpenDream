/proc/RunTest()	
	var/database/db = new("database.db")

	var/database/query/query = new("CREATE TABLE test (id int, name string, points float)")
	query.Execute(db)

	query.Add("INSERT INTO test VALUES (?, ?, ?)", 1, "foo", 1.5)
	query.Execute(db)

	ASSERT(query.RowsAffected() == 1)

	query.Add("SELECT * FROM test WHERE id = ?", 1)
	query.Execute(db)
	query.NextRow()

	var/list/assoc = query.GetRowData()
	ASSERT(length(assoc) == 3)
	
	ASSERT(assoc["id"] == 1)
	ASSERT(assoc["name"] == "foo")
	ASSERT(assoc["points"] == 1.5)

	fdel("database.db")
