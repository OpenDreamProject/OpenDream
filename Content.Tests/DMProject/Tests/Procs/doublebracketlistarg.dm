/proc/have_fun(ways_to_have_fun[] = list("dancing", "jumping around", "sightseeing"))
  return ways_to_have_fun[2]

/proc/emptylistproc(emptylist[] = null)
  return emptylist
  
/proc/RunTest()
  ASSERT(have_fun() == "jumping around")
  ASSERT(have_fun(list("eating cake", "spinning")) == "spinning")
  ASSERT(isnull(emptylistproc()))
