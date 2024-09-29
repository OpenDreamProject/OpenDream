/proc/RunTest()
	var/database/db = new("noentries.db")

	var/database/query/query = new("CREATE TABLE foobar (id int)")
	query.Execute(db)
	
	query.Add("SELECT * FROM foobar")
	query.Execute(db)
	query.NextRow()
	
	query.GetRowData()
	
	ASSERT(query.Error() && query.ErrorMsg())
	
	del(query)
	del(db)
	
	fdel("noentries.db")