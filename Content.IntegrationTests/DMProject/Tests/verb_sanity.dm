#define VERB_IN(identifier, the_src) verb/in_##identifier(){set name = "src in " + #the_src; set src in the_src;}
#define VERB_EQUALS(identifier, the_src) verb/equals_##identifier(){set name = "src = " + #the_src; set src = the_src;}
#define VERB_SUITE(...) \
	VERB_IN(usr, usr) \
	VERB_IN(usr_loc, usr.loc) \
	VERB_IN(usr_contents, usr.contents) \
	VERB_IN(usr_group, usr.group) \
	VERB_IN(view, view(0)) \
	VERB_IN(oview, oview(0)) \
	VERB_EQUALS(usr, usr) \
	VERB_EQUALS(usr_loc, usr.loc) \
	VERB_EQUALS(usr_contents, usr.contents) \
	VERB_EQUALS(usr_group, usr.group) \
	VERB_EQUALS(view, view(0)) \
	VERB_EQUALS(oview, oview(0)) \

/obj/verb_holder
	VERB_SUITE()

/mob/verb_holder
	VERB_SUITE()

/datum/unit_test/verb_sanity/proc/get_available_verbs(client/client, datum/target)
	// done like this because we don't actually track what verbs a mob can use on the server
	var/static/list/every_possible_verb = list(
		/obj/verb_holder/proc/in_usr,
		/obj/verb_holder/proc/in_usr_loc,
		/obj/verb_holder/proc/in_usr_contents,
		// /obj/verb_holder/proc/in_usr_group,
		/obj/verb_holder/proc/in_view,
		/obj/verb_holder/proc/in_oview,
		/obj/verb_holder/proc/equals_usr,
		/obj/verb_holder/proc/equals_usr_loc,
		/obj/verb_holder/proc/equals_usr_contents,
		// /obj/verb_holder/proc/equals_usr_group,
		/obj/verb_holder/proc/equals_view,
		/obj/verb_holder/proc/equals_oview,
		
		/mob/verb_holder/proc/in_usr,
		/mob/verb_holder/proc/in_usr_loc,
		/mob/verb_holder/proc/in_usr_contents,
		// /mob/verb_holder/proc/in_usr_group,
		/mob/verb_holder/proc/in_view,
		/mob/verb_holder/proc/in_oview,
		/mob/verb_holder/proc/equals_usr,
		/mob/verb_holder/proc/equals_usr_loc,
		/mob/verb_holder/proc/equals_usr_contents,
		// /mob/verb_holder/proc/equals_usr_group,
		/mob/verb_holder/proc/equals_view,
		/mob/verb_holder/proc/equals_oview,
	)

	return Debug.GetExecutableVerbs(client, target, every_possible_verb)

/datum/unit_test/verb_sanity/proc/assert_verbs(client/client, datum/target, list/verb_paths)
	var/list/available_verbs = get_available_verbs(client, target)
	var/list/found_verbs = available_verbs & verb_paths
	var/list/missing_verbs = available_verbs - verb_paths
	var/list/excessive_verbs = verb_paths - available_verbs

	if(missing_verbs.len)
		var/list/error_output = list()
		error_output += "(client -> [target]) had incorrect verb count"
		error_output += "had:"
		for(var/verb in found_verbs)
			error_output += "\t[verb]"
		if(missing_verbs.len)
			error_output += "missing:"
			for(var/verb in missing_verbs)
				error_output += "\t[verb]"
		if(excessive_verbs.len)
			error_output += "excessive:"
			for(var/verb in excessive_verbs)
				error_output += "\t[verb]"
		CRASH(error_output.Join("\n"))

/datum/unit_test/verb_sanity/RunTest()
	var/client/client
	for(var/client/found_client)
		client = found_client
		break
	var/mob/client_mob = client.mob
	
	var/obj/verb_holder/container = new()
	container.name = "\proper container_obj"
	var/list/expected_container_verbs = list(
		/obj/verb_holder/proc/in_usr_loc,
		/obj/verb_holder/proc/equals_usr_loc,
		/obj/verb_holder/proc/in_view,
		/obj/verb_holder/proc/equals_view,
		/obj/verb_holder/proc/in_oview,
		/obj/verb_holder/proc/equals_oview,
	)

	var/mob/verb_holder/mob = new(container)
	mob.name = "\proper src_mob"
	var/list/expected_mob_verbs = list(
		/mob/verb_holder/proc/equals_usr,
		/mob/verb_holder/proc/in_view,
		/mob/verb_holder/proc/equals_view,
	)

	var/obj/verb_holder/contained = new(mob)
	contained.name = "\proper contained_obj"
	var/list/expected_contained_verbs = list(
		/obj/verb_holder/proc/in_usr,
		/obj/verb_holder/proc/in_usr_contents,
		/obj/verb_holder/proc/equals_usr_contents,
		/obj/verb_holder/proc/in_view,
		/obj/verb_holder/proc/equals_view,
		/obj/verb_holder/proc/in_oview,
		/obj/verb_holder/proc/equals_oview,
	)

	client.mob = mob
	assert_verbs(client, container, expected_container_verbs)
	assert_verbs(client, mob, expected_mob_verbs)
	assert_verbs(client, contained, expected_contained_verbs)
	client.mob = client_mob
