/proc/RunTest()
	var/database/db = new("noentries.db")

	var/database/query/query = new("CREATE TABLE foobar (id int)")
	query.Execute(db)
	
	query.Add("SELECT * FROM foobar")
	query.Execute(db)
	ASSERT(!query.NextRow())
	
	ASSERT(query.GetRowData()["id"] == null)
	
	ASSERT(!query.Error())
	
	del(query)
	del(db)
	
	fdel("noentries.db")