
/proc/RunTest()
	var/list/all = world.GetConfig("env")
	ASSERT(islist(all))
	ASSERT(all.len > 5)

	var/path = world.GetConfig("env", "PATH")
	ASSERT(istext(path) && length(path))

	var/env_var = "SUPER_SPECIAL_DM_ENVIRONMENT_KEY_FOR_TESTING"
	var/env_value = "test value"
	ASSERT(world.GetConfig("env", env_var) == null)
	world.SetConfig("env", env_var, env_value)
	var/retrieved = world.GetConfig("env", env_var)
	ASSERT(retrieved == env_value)
	ASSERT(retrieved != null)
	world.SetConfig("env", env_var, null)
	ASSERT(world.GetConfig("env", env_var) == null)
