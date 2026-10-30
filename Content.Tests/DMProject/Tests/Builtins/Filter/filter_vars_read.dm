// Filter vars read as DM values of each kind
// Only vars the specific filter type has can be read or written
/proc/RunTest()
	var/dm_filter/F = filter(type="blur", size=3, name="foo")
	ASSERT(F.type == "blur")
	ASSERT(F:name == "foo")
	ASSERT(F.size == 3)
	ASSERT(filter(type="blur"):size == 1) // Default

	F = filter(type="outline", color="#FF0000")
	ASSERT(F.color == "#ff0000")
	ASSERT(filter(type="drop_shadow"):color == "#00000080")

	F = filter(type="color", color="#ff0000")
	var/list/color_matrix = F.color
	ASSERT(color_matrix ~= list(1,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,1, 0,0,0,0))

	F = filter(type="layer", transform=matrix(2, 0, 3, 0, 4, 5))
	var/matrix/M = F:transform
	ASSERT(M.a == 2 && M.c == 3 && M.e == 4 && M.f == 5)

	// A standalone filter can be written
	F = filter(type="blur", size=1)
	F.size = 4
	ASSERT(F.size == 4)

	// `name` and vars the filter type doesn't have are runtime errors
	var/failures = 0
	try
		F:x = 3
	catch
		failures++
	try
		var/x = F:x
	catch
		failures++
	try
		F:name = "foo"
	catch
		failures++
	ASSERT(failures == 3)
