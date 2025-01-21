// COMPILE ERROR OD0407

/datum
	var/final/foo = 1

/datum/a
	foo = 2 // Can't override a final var