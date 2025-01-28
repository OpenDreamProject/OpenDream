/datum/thing
	var/name = "thing"

/datum/Thing
	var/name = "Thing"

/datum/proper_thing
	var/name = "\proper thing"

/datum/plural_things
	var/name = "things"
	var/gender = PLURAL

/proc/RunTest()
	// Lowercase \a on datums
	ASSERT("\a [new /datum/thing]" == "a thing")
	ASSERT("\a [new /datum/Thing]" == "Thing")
	ASSERT("\a [new /datum/proper_thing]" == "thing")
	ASSERT("\a [new /datum/plural_things]" == "some things")
	
	// Uppercase \A on datums
	ASSERT("\A [new /datum/thing]" == "A thing")
	ASSERT("\A [new /datum/Thing]" == "Thing")
	ASSERT("\A [new /datum/proper_thing]" == "thing")
	ASSERT("\A [new /datum/plural_things]" == "Some things")
	
	// Lowercase \a on strings
	ASSERT("\a ["thing"]" == "a thing")
	ASSERT("\a ["Thing"]" == "Thing")
	ASSERT("\a ["\proper thing"]" == "thing")
	
	// Uppercase \A on strings
	ASSERT("\A ["thing"]" == "A thing")
	ASSERT("\A ["Thing"]" == "Thing")
	ASSERT("\A ["\proper thing"]" == "thing")

	// Invalid \a
	ASSERT("\a [123]" == "")
	ASSERT("\A [123]" == "")