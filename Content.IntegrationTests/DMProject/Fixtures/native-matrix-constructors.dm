var/matrix_new_calls = 0
var/matrix_error_calls = 0

/matrix/New()
    matrix_new_calls++

/world/Error(exception/failure)
    matrix_error_calls++
    world.log << "MATRIX_EXPECTED_RUNTIME: [failure.name]"

/proc/matrix_constructor_valid()
    return matrix(1, 2, 3, 4, 5, 6)

/proc/matrix_constructor_invalid()
    return matrix(null, 2, 3, 4, 5, 6)

/proc/matrix_constructor_counts()
    return matrix_new_calls * 10 + matrix_error_calls
