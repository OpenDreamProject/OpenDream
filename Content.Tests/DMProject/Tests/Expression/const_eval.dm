/datum
	var/composite_expr = 4 * sin(3 + cos(2))
	var/sintest = sin(45)
	var/costest = cos(123)
	var/tantest = tan(123)
	var/sqrttest = sqrt(123)
	var/arcsintest = arcsin(sin(45))
	var/arccostest = arccos(cos(123))
	var/arctantest = arctan(tan(69))
	var/log_test = log(10)
	var/log10_test = log(10, 100)
	var/arctan2_test = arctan(1, 3)
	var/abs_test = abs(-213)

/proc/RunTest()
	var/break_const_eval = null
	var/datum/d = new /datum

	ASSERT(initial(d.composite_expr) == 4 * sin(3 + cos(break_const_eval || 2)))
	ASSERT(d.composite_expr == 4 * sin(3 + cos(break_const_eval || 2)))

	ASSERT(d.sintest == sin(break_const_eval || 45))
	ASSERT(d.sintest == 0.707106769084930419921875)

	ASSERT(d.costest == cos(break_const_eval || 123))
	ASSERT(d.costest == -0.544639050960540771484375)

	ASSERT(d.tantest == tan(break_const_eval || 123))
	if (d.tantest != -1.539865016937255859375)
		CRASH("tantest error: [d.tantest - (-1.539865016937255859375)]")
	// ASSERT(d.tantest == -1.539865016937255859375)

	ASSERT(d.sqrttest == sqrt(break_const_eval || 123))
	ASSERT(d.sqrttest == 11.0905361175537109375)

	ASSERT(d.arcsintest == arcsin(sin(break_const_eval || 45)))
	ASSERT(d.arcsintest == 45)

	ASSERT(d.arccostest == arccos(cos(break_const_eval || 123)))
	ASSERT(d.arccostest == 123)

	ASSERT(d.arctantest == arctan(tan(break_const_eval || 69)))
	ASSERT(d.arctantest == 69)

	ASSERT(d.log_test == log(break_const_eval || 10))
	ASSERT(d.log_test == 2.302585124969482421875)

	ASSERT(d.log10_test == log(break_const_eval || 10, 100))
	ASSERT(d.log10_test == 2)

	ASSERT(d.arctan2_test == arctan(break_const_eval || 1, 3))
	ASSERT(d.arctan2_test == 71.5650482177734375)

	ASSERT(d.abs_test == abs(break_const_eval || -213))
	ASSERT(d.abs_test == 213)