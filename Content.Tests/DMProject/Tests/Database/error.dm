/proc/RunTest()
	var/database/db = new("database.db")

	var/database/query/query = new("I am the greatest SQL query writer of all time.")
	query.Execute(db)

	ASSERT(query.Error() == 1)
	ASSERT(length(query.ErrorMsg()) > 1)

	ASSERT(db.Error() == 1)
	ASSERT(length(db.ErrorMsg()) > 1)

	fdel("database.db")