/matrix/proc/operator*(item) //it should not be possible to override an internal operator overload
  return list(item)

/proc/RunTest()
  var/matrix/thing = new()
  thing = thing * "scream"
  ASSERT(json_encode(thing) == "\[0,0,0,0,0,0\]")

