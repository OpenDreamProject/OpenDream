// IGNORE

// A helper method for testing operators against many kinds of values

/datum/proc/foo()
/datum/verb/bar()

/proc/test_operator(var/operator_proc)
	var/list/values = list(
		10,
		"ABC",
		null,
		'file.txt',
		list("A"),
		new /datum(),
		/datum,
		/datum/proc/foo,
		/datum/verb/bar,
		/datum/proc,
		/datum/verb
	)
    
	var/list/results = list()
	for (var/a in values)
		for (var/b in values)
			try
				var/result = call(operator_proc)(a, b)
				
				if (islist(result))
					results += list(result)
				else
					results += result
			catch
				results += "Error"
	
	return results