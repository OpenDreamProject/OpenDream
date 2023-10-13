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

#define EPSILON 4e-6
#define APX_EQUAL(a, b) ASSERT(abs(a - b) < EPSILON)

/proc/RunTest()
	var/break_const_eval = null
	var/datum/d = new /datum

	APX_EQUAL(initial(d.composite_expr), 4 * sin(3 + cos(break_const_eval || 2)))
	APX_EQUAL(d.composite_expr, 4 * sin(3 + cos(break_const_eval || 2)))

	APX_EQUAL(d.sintest, sin(break_const_eval || 45))
	APX_EQUAL(d.sintest, 0.707106769084930419921875)

	APX_EQUAL(d.costest, cos(break_const_eval || 123))
	APX_EQUAL(d.costest, -0.544639050960540771484375)

	APX_EQUAL(d.tantest, tan(break_const_eval || 123))
	APX_EQUAL(d.tantest, -1.539865016937255859375)

	APX_EQUAL(d.sqrttest, sqrt(break_const_eval || 123))
	APX_EQUAL(d.sqrttest, 11.0905361175537109375)

	APX_EQUAL(d.arcsintest, arcsin(sin(break_const_eval || 45)))
	APX_EQUAL(d.arcsintest, 45)

	APX_EQUAL(d.arccostest, arccos(cos(break_const_eval || 123)))
	APX_EQUAL(d.arccostest, 123)

	APX_EQUAL(d.arctantest, arctan(tan(break_const_eval || 69)))
	APX_EQUAL(d.arctantest, 69)

	APX_EQUAL(d.log_test, log(break_const_eval || 10))
	APX_EQUAL(d.log_test, 2.302585124969482421875)

	APX_EQUAL(d.log10_test, log(break_const_eval || 10, 100))
	APX_EQUAL(d.log10_test, 2)

	APX_EQUAL(d.arctan2_test, arctan(break_const_eval || 1, 3))
	APX_EQUAL(d.arctan2_test, 71.5650482177734375)

	APX_EQUAL(d.abs_test, abs(break_const_eval || -213))
	