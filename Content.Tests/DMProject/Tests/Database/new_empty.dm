// RUNTIME ERROR

/proc/RunTest()
	var/database/db = new()
	var/database/query/query = new("CREATE TABLE foobar (id int)")

	query.Execute(db)