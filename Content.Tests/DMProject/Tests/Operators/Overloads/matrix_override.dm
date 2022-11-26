/matrix/proc/operator*(item)
  return list(item)

/proc/RunTest()
  var/matrix/thing = new()
  thing = thing * "scream"
  ASSERT(json_encode(thing) == "\[0,0,0,0,0,0\]")

