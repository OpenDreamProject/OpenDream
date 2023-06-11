
/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)
	var/matrix/N = matrix(7, 8, 9, 10, 11, 12)

	M.Subtract(N)

	if(M ~! matrix(-6, -6, -6, -6, -6, -6))
		CRASH("Unexpected matrix/Subtract result: [json_encode(M)]")
		
	M = M - N

	if(M ~! matrix(-13,-14,-15,-16,-17,-18))
		CRASH("Unexpected matrix/OperatorSubtract result: [json_encode(M)]")
		
	M -= N

	if(M ~! matrix(-20,-22,-24,-26,-28,-30))
		CRASH("Unexpected matrix/OperatorRemove result: [json_encode(M)]")