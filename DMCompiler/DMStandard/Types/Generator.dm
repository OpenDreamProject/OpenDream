//TODO: Figure out how generators work internally

/generator
	parent_type = /datum
	var/_binobj as opendream_unimplemented

/generator/proc/Rand()
	set opendream_unimplemented = TRUE

/*
Generator Theory

Generators seem to have a "_binobj" var that stores the proc used to create the generator with the relevant args

That is somehow used in Rand() to return a relevant random value
*/
