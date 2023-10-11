// IGNORE

// A helper method for testing operators against many kinds of values

/var/datum/operator_test_object = new /datum()

/datum/proc/foo()
/datum/verb/bar()

/proc/test_operator(var/operator_proc, var/list/expected)
	var/list/values = list(
		10,
		"ABC",
		null,
		'file.txt',
		list("A"),
		operator_test_object,
		/datum,
		/datum/proc/foo,
		/datum/verb/bar,
		/datum/proc,
		/datum/verb
	)
    
	var/i = 1
	for (var/a in values)
		for (var/b in values)
			var/expected_result = expected[i++]
			var/result
			
			try
				result = call(operator_proc)(a, b)
			catch
				result = "Error"
			
			if (!(result ~= expected_result))
				CRASH("Expected [json_encode(expected_result)] for [json_encode(a)] + [json_encode(b)], instead got [json_encode(result)]")
