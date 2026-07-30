// atom.color has some peculiar handling, notably being tied to atom.alpha and color matrices
#define COLOR_ASSERT(bool) assert((bool), "Assertion Failed: [#bool]")

/datum/unit_test/appearance_color
	var/list/runtimes = list()

/datum/unit_test/appearance_color/proc/assert(condition, errmsg)
	try
		if(!condition)
			throw EXCEPTION(errmsg)
	catch(var/exception/e)
		runtimes += e.name

/datum/unit_test/appearance_color/RunTest()
	var/mob/M = new
	COLOR_ASSERT(isnull(M.color))
	COLOR_ASSERT(M.alpha == 255)

	M.color = "#ffffff"
	COLOR_ASSERT(isnull(M.color))
	COLOR_ASSERT(M.alpha == 255)

	M.color = "transparent"
	COLOR_ASSERT(M.color == "#000000")
	COLOR_ASSERT(M.alpha == 0)

	// RGBA should trim the alpha component and assign it to atom.alpha
	M.color = "#ff0000c8"
	COLOR_ASSERT(M.color == "#ff0000")
	COLOR_ASSERT(M.alpha == 200)

	// Assigning a non-list instance should just do nothing
	M.color = /datum
	COLOR_ASSERT(M.color == "#ff0000")
	COLOR_ASSERT(M.alpha == 200)

	// Null should be allowed and be equivalent to "#ffffff"
	M.color = null
	COLOR_ASSERT(isnull(M.color))
	COLOR_ASSERT(M.alpha == 200)

	var/list/identity_matrix = list(
		1, 0, 0, 0,
		0, 1, 0, 0,
		0, 0, 1, 0,
		0, 0, 0, 1,
		0, 0, 0, 0)
	M.color = identity_matrix
	COLOR_ASSERT(isnull(M.color))
	COLOR_ASSERT(M.alpha == 255)

	// Matrices should affect the alpha
	var/list/transparent_matrix = list(
		1, 0, 0, 0,
		0, 1, 0, 0,
		0, 0, 1, 0,
		0, 0, 0, 0.5,
		0, 0, 0, 0)
	M.color = transparent_matrix
	COLOR_ASSERT(isnull(M.color))
	COLOR_ASSERT(M.alpha == 128)

	// Specifying valid RGBA should condense the matrix into text
	var/list/condensable_matrix = list(
		1, 0, 0,
		0, 0, 0,
		0, 0, 0)
	M.color = condensable_matrix
	COLOR_ASSERT(M.color == "#ff0000")
	COLOR_ASSERT(M.alpha == 255)

	// Specifying invalid RGBA should not condense the matrix and should force var/alpha to 255
	var/list/peculiar_matrix = list(
		1, 0, 0.5, 0, // rb
		0, 1, 0, 0,
		0, 0, 1, 0,
		0, 0, 0, 0.5,
		0, 0, 0, 0)
	M.color = peculiar_matrix
	COLOR_ASSERT(M.color ~= peculiar_matrix)
	COLOR_ASSERT(M.alpha == 255)

	// Having a non-condensable matrix sets the alpha of the matrix
	M.alpha = 51
	var/list/peculiar_alpha_matrix = list(
		1, 0, 0.5, 0, // rb
		0, 1, 0, 0,
		0, 0, 1, 0,
		0, 0, 0, 0.2,
		0, 0, 0, 0)
	COLOR_ASSERT(M.color ~= peculiar_alpha_matrix)
	COLOR_ASSERT(M.alpha == 255)

	// test is done, output the errors
	del(M)
	if(length(runtimes))
		CRASH("[length(runtimes)] exceptions:\n\t[runtimes.Join("\n\t")]")

#undef COLOR_ASSERT