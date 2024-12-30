// IGNORE

// A helper method for testing operators against many kinds of values

/var/datum/operator_test_object = new /datum()

/datum/proc/foo()
/datum/verb/bar()

var/list/operator_test_values = list(
	10,
	"ABC",
	null,
	'file.txt',
	list("ABC"),
	operator_test_object,
	/datum,
	/datum/proc/foo,
	/datum/verb/bar,
	/datum/proc,
	/datum/verb
)

/proc/test_unary_operator(var/operator_proc, var/list/expected)
	var/i = 1
	for (var/a in operator_test_values)
		var/expected_result = expected[i++]
		var/result
		
		try
			result = call(operator_proc)(a)
		catch
			result = "Error"
		
		if (result ~! expected_result)
			CRASH("Expected [json_encode(expected_result)] for [json_encode(a)], instead got [json_encode(result)] at index [i - 1]")

/proc/test_binary_operator(var/operator_proc, var/list/expected)
	var/i = 1
	for (var/a in operator_test_values)
		for (var/b in operator_test_values)
			var/expected_result = expected[i++]
			var/result
			
			try
				result = call(operator_proc)(a, b)
			catch
				result = "Error"
			
			if (result ~! expected_result)
				CRASH("Expected [json_encode(expected_result)] for [json_encode(a)] and [json_encode(b)], instead got [json_encode(result)] at index [i - 1]")
