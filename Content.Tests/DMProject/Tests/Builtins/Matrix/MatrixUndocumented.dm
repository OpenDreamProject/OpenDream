//Below are some tests to make sure our impl of the undocumented /matrix() signatures actually works.
// Issue OD#1055: https://github.com/OpenDreamProject/OpenDream/issues/1055

/proc/round_but_todo_please_fix_this_inaccuracy(mat)
	mat:a = round(mat:a,0.00001)
	mat:b = round(mat:b,0.000001)
	mat:c = round(mat:c,0.000001)
	mat:d = round(mat:d,0.000001)
	mat:e = round(mat:e,0.000001)
	mat:f = round(mat:f,0.000001)
	return mat

/proc/RunTest()
	//MATRIX_COPY
	var/matrix/doReMe = matrix(1,2,3,4,5,6)
	var/matrix/copyMatrix = matrix(doReMe,MATRIX_COPY)
	if(doReMe ~! copyMatrix)
		CRASH("MATRIX_COPY failed to copy a matrix, got [copyMatrix] instead.")
	if(doReMe == copyMatrix)
		CRASH("MATRIX_COPY failed to copy a matrix, got the same matrix back instead.")
	
	//MATRIX_SCALE
	ASSERT(matrix(2,MATRIX_SCALE) ~= matrix(2,0,0,0,2,0))
	ASSERT(matrix(2,3,MATRIX_SCALE) ~= matrix(2,0,0,0,3,0))

	//MATRIX_TRANSLATE
	ASSERT(matrix(2,MATRIX_TRANSLATE) ~= matrix(1,0,2,0,1,2)) // completely undocumented, but, whatever.
	ASSERT(matrix(2,3,MATRIX_TRANSLATE) ~= matrix(1,0,2,0,1,3))
	ASSERT(matrix(doReMe,2,3,MATRIX_TRANSLATE) ~= matrix(1,2,5,4,5,9))
	if(doReMe ~! matrix(1,2,3,4,5,6))
		CRASH("MATRIX_TRANSLATE modified the matrix it was given, without being given a MATRIX_MODIFY flag.")

	//MATRIX_ROTATE
	var/matrix/rotated = matrix(90, MATRIX_ROTATE)
	//TODO: Fix discrepancy in our trigonometry results
	rotated = round_but_todo_please_fix_this_inaccuracy(rotated)
	if(rotated ~! matrix(0,1,0,-1,0,0))
		CRASH("MATRIX_ROTATE failure, expected \[0,1,0,-1,0,0\], got [json_encode(matrix(90, MATRIX_ROTATE))]")
	
	//MATRIX_ROTATE with matrix argument
	var/matrix/before_rotate = matrix(1,2,3,4,5,6)
	var/matrix/rotated2 = matrix(before_rotate, 90, MATRIX_ROTATE)
	rotated2 = round_but_todo_please_fix_this_inaccuracy(rotated2)
	if(rotated2 ~! matrix(4,5,6,-1,-2,-3))
		CRASH("MATRIX_ROTATE failure, expected \[4,5,6,-1,-2,-3\], got [json_encode(rotated2)]")
	if(before_rotate ~! matrix(1,2,3,4,5,6))
		CRASH("MATRIX_ROTATE modified the matrix it was given, without being given a MATRIX_MODIFY flag.")
	matrix(before_rotate, 90, MATRIX_ROTATE | MATRIX_MODIFY)
	if(before_rotate ~! matrix(4,5,6,-1,-2,-3))
		CRASH("MATRIX_ROTATE | MATRIX_MODIFY failed to leave a modified, rotated matrix. Matrix is instead [json_encode(before_rotate)].")

	//MATRIX_INVERT
	var/matrix/inv = matrix(doReMe,MATRIX_INVERT)
	if(inv ~! matrix(-5/3,2/3,1,4/3,-1/3,-2))
		CRASH("MATRIX_INVERT failed. Expected [json_encode(matrix(-5/3,2/3,1,4/3,-1/3,-2))], got [json_encode(inv)]")

	//MATRIX_MODIFY
	matrix(doReMe,MATRIX_INVERT | MATRIX_MODIFY)
	if(doReMe ~! matrix(-5/3,2/3,1,4/3,-1/3,-2))
		CRASH("MATRIX_INVERT | MATRIX_MODIFY failed to leave a modified, inverted matrix. Matrix is instead [json_encode(doReMe)].")
