
/mob
	var/species_alignment
	dragon
		species_alignment = .dragon
		red
			redder
		black
			species_alignment = .black
	snake
		species_alignment = .snake
		cobra
		winged
			species_alignment = .dragon
		pit_viper
			species_alignment = .dragon/black
		red_snek
			species_alignment = .dragon:redder

/proc/RunTest()
	var/mob/snake/red_snek/snek = new /mob:pit_viper

	var/list/types = typesof(snek)
	ASSERT(types.len == 1)
	ASSERT(snek.species_alignment == /mob/dragon/black)
