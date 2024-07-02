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

	ASSERT(query.GetColumn(0) == 1)
	ASSERT(query.GetColumn(1) == "foo")
	ASSERT(query.GetColumn(2) == 1.5)
	
	var/list/columns = query.Columns()	
	ASSERT(columns[1] == "id")
	ASSERT(columns[2] == "name")
	ASSERT(columns[3] == "points")
	
	ASSERT(query.Columns(0) == "id")
	ASSERT(query.Columns(1) == "name")
	ASSERT(query.Columns(2) == "points")
	
	ASSERT(!query.Columns(10))

	ASSERT(query.Error() && query.ErrorMsg())
	
	query.Close()
	db.Close()
	
	db.Open("database.db")
	
	query.Add("SELECT * FROM test WHERE id = ?", 1)
	query.Execute(db)
	query.NextRow()
	
	ASSERT(query.GetColumn(0) == 1)
	
	ASSERT(!query.GetColumn(10))
	ASSERT(query.Error() && query.ErrorMsg())

	fdel("database.db")
