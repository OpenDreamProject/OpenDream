/proc/RunTest()
	var/database/db = new("foo.db")
	
	var/database/query/query = new("CREATE TABLE foo (id int)")
	query.Execute(db)
	
	query.Add("INSERT INTO foo VALUES (1)")
	query.Execute(db)
	
	ASSERT(query.RowsAffected() == 1)
	
	db.Open("bar.db")
	query.Add("CREATE TABLE bar (id int)")
	query.Execute(db)
	
	query.Add("INSERT INTO bar VALUES (1)")
	query.Execute(db)
	
	ASSERT(query.RowsAffected() == 1)
	
	del(query)
	del(db)

	fdel("foo.db")
	fdel("bar.db")