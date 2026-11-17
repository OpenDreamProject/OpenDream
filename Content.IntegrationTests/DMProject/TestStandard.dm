/debug_hooks
	proc/CanExecuteVerb(client/client, datum/target, verb)
	proc/GetExecutableVerbs(client/client, datum/target, list/possible_verbs)

var/global/debug_hooks/Debug = new /debug_hooks()