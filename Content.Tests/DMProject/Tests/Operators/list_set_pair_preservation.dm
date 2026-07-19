
/proc/xor_assign_scalar(var/list/target, value)
	return target ^= value

/proc/RunTest()
	var/list/numeric_left = list(7, 1, 7, 9)
	var/list/numeric_mask = numeric_left & list(7, 9)
	ASSERT(numeric_mask.len == 2)
	ASSERT(numeric_mask[1] == 7)
	ASSERT(numeric_mask[2] == 9)
	ASSERT(numeric_left.len == 4)

	var/list/scalar_mask = numeric_left & 7
	ASSERT(scalar_mask.len == 1)
	ASSERT(scalar_mask[1] == 7)

	var/list/assoc_left = list("a" = 10, "b" = 20, "c" = 30)
	var/list/assoc_mask = assoc_left & list("c", "a")
	ASSERT(assoc_mask.len == 2)
	ASSERT(assoc_mask[1] == "a")
	ASSERT(assoc_mask[2] == "c")
	ASSERT(assoc_mask["a"] == 10)
	ASSERT(assoc_mask["c"] == 30)
	ASSERT(!("b" in assoc_mask))
	ASSERT(assoc_left["b"] == 20)

	var/list/numeric_mask_assign = list(7, 1, 7, 9)
	var/list/numeric_mask_assign_original = numeric_mask_assign
	var/numeric_mask_assign_result = (numeric_mask_assign &= list(7, 9))
	ASSERT(numeric_mask_assign_result == numeric_mask_assign_original)
	ASSERT(numeric_mask_assign.len == 2)
	ASSERT(numeric_mask_assign[1] == 7)
	ASSERT(numeric_mask_assign[2] == 9)

	var/list/assoc_mask_assign = list("a" = 10, "b" = 20, "c" = 30)
	var/list/assoc_mask_assign_original = assoc_mask_assign
	var/assoc_mask_assign_result = (assoc_mask_assign &= list("c", "a"))
	ASSERT(assoc_mask_assign_result == assoc_mask_assign_original)
	ASSERT(assoc_mask_assign.len == 2)
	ASSERT(assoc_mask_assign["a"] == 10)
	ASSERT(assoc_mask_assign["c"] == 30)
	ASSERT(!("b" in assoc_mask_assign))

	var/list/or_left = list("a" = 10, "b" = 20)
	var/list/or_result = or_left | list("b" = 200, "c" = 300)
	ASSERT(or_result.len == 3)
	ASSERT(or_result[1] == "a")
	ASSERT(or_result[2] == "b")
	ASSERT(or_result[3] == "c")
	ASSERT(or_result["a"] == 10)
	ASSERT(or_result["b"] == 20)
	ASSERT(or_result["c"] == 300)
	ASSERT(or_left["b"] == 20)

	var/list/xor_numeric = list(7, 1, 7) ^ list(1, 9)
	ASSERT(xor_numeric.len == 3)
	ASSERT(xor_numeric[1] == 7)
	ASSERT(xor_numeric[2] == 7)
	ASSERT(xor_numeric[3] == 9)

	var/list/xor_assoc = list("a" = 10, "b" = 20) ^ list("b" = 200, "c" = 300)
	ASSERT(xor_assoc.len == 2)
	ASSERT(xor_assoc[1] == "a")
	ASSERT(xor_assoc[2] == "c")
	ASSERT(xor_assoc["a"] == 10)
	ASSERT(xor_assoc["c"] == 300)
	ASSERT(!("b" in xor_assoc))

	var/list/xor_assign_numeric = list(7, 1, 7)
	var/list/xor_assign_numeric_original = xor_assign_numeric
	var/xor_assign_numeric_result = (xor_assign_numeric ^= list(1, 9))
	ASSERT(xor_assign_numeric_result == xor_assign_numeric)
	ASSERT(xor_assign_numeric == xor_assign_numeric_original)
	ASSERT(xor_assign_numeric.len == 3)
	ASSERT(xor_assign_numeric[1] == 7)
	ASSERT(xor_assign_numeric[2] == 7)
	ASSERT(xor_assign_numeric[3] == 9)

	var/list/xor_assign_assoc = list("a" = 10, "b" = 20)
	var/list/xor_assign_assoc_original = xor_assign_assoc
	var/xor_assign_assoc_result = (xor_assign_assoc ^= list("b" = 200, "c" = 300))
	ASSERT(xor_assign_assoc_result == xor_assign_assoc)
	ASSERT(xor_assign_assoc == xor_assign_assoc_original)
	ASSERT(xor_assign_assoc.len == 2)
	ASSERT(xor_assign_assoc[1] == "a")
	ASSERT(xor_assign_assoc[2] == "c")
	ASSERT(xor_assign_assoc["a"] == 10)
	ASSERT(xor_assign_assoc["c"] == 300)
	ASSERT(!("b" in xor_assign_assoc))

	var/list/xor_assign_scalar_present = list(7, 1, 7)
	var/list/xor_assign_scalar_present_original = xor_assign_scalar_present
	var/xor_assign_scalar_present_result = (xor_assign_scalar_present ^= 7)
	ASSERT(xor_assign_scalar_present_result == xor_assign_scalar_present)
	ASSERT(xor_assign_scalar_present == xor_assign_scalar_present_original)
	ASSERT(xor_assign_scalar_present.len == 2)
	ASSERT(xor_assign_scalar_present[1] == 1)
	ASSERT(xor_assign_scalar_present[2] == 7)

	var/list/xor_assign_scalar_missing = list(7, 1, 7)
	var/list/xor_assign_scalar_missing_original = xor_assign_scalar_missing
	var/xor_assign_scalar_missing_result = (xor_assign_scalar_missing ^= 9)
	ASSERT(xor_assign_scalar_missing_result == xor_assign_scalar_missing)
	ASSERT(xor_assign_scalar_missing == xor_assign_scalar_missing_original)
	ASSERT(xor_assign_scalar_missing.len == 4)
	ASSERT(xor_assign_scalar_missing[4] == 9)

	var/list/xor_assign_assoc_scalar_present = list("a" = 10, "b" = 20)
	var/list/xor_assign_assoc_scalar_present_original = xor_assign_assoc_scalar_present
	var/xor_assign_assoc_scalar_present_result = xor_assign_scalar(xor_assign_assoc_scalar_present, "b")
	ASSERT(xor_assign_assoc_scalar_present_result == xor_assign_assoc_scalar_present)
	ASSERT(xor_assign_assoc_scalar_present == xor_assign_assoc_scalar_present_original)
	ASSERT(xor_assign_assoc_scalar_present.len == 1)
	ASSERT(xor_assign_assoc_scalar_present["a"] == 10)
	ASSERT(!("b" in xor_assign_assoc_scalar_present))

	var/list/xor_assign_assoc_scalar_missing = list("a" = 10, "b" = 20)
	var/list/xor_assign_assoc_scalar_missing_original = xor_assign_assoc_scalar_missing
	var/xor_assign_assoc_scalar_missing_result = xor_assign_scalar(xor_assign_assoc_scalar_missing, "c")
	ASSERT(xor_assign_assoc_scalar_missing_result == xor_assign_assoc_scalar_missing)
	ASSERT(xor_assign_assoc_scalar_missing == xor_assign_assoc_scalar_missing_original)
	ASSERT(xor_assign_assoc_scalar_missing.len == 3)
	ASSERT(xor_assign_assoc_scalar_missing["a"] == 10)
	ASSERT(xor_assign_assoc_scalar_missing["b"] == 20)
	ASSERT(("c" in xor_assign_assoc_scalar_missing))
	ASSERT(xor_assign_assoc_scalar_missing["c"] == null)

	return 
