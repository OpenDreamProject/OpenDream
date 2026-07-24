var/global/dimension_events = 0
var/global/first_dimension_calls = 0
var/global/second_dimension_calls = 0

/proc/make_one_dimension(value)
	var/list/L[value]
	return L

/proc/make_two_dimensions(first, second)
	var/list/L[first][second]
	return L

/proc/construct_one_dimension(value)
	return new /list(value)

/proc/construct_two_dimensions(first, second)
	return new /list(first, second)

/proc/first_dimension()
	first_dimension_calls++
	dimension_events = dimension_events * 10 + 1
	return 2

/proc/second_dimension()
	second_dimension_calls++
	dimension_events = dimension_events * 10 + 2
	return 3

/proc/RunTest()
	ASSERT(isnull(make_one_dimension(-1)))

	var/list/negative_fraction = make_one_dimension(-0.5)
	var/list/positive_fraction = make_one_dimension(1.5)
	var/list/null_size = make_one_dimension(null)
	var/list/text_size = make_one_dimension("2")
	var/list/datum_size = make_one_dimension(new /datum)
	var/list/list_size = make_one_dimension(list(2))
	ASSERT(negative_fraction.len == 0)
	ASSERT(positive_fraction.len == 1)
	ASSERT(null_size.len == 0)
	ASSERT(text_size.len == 0)
	ASSERT(datum_size.len == 0)
	ASSERT(list_size.len == 0)

	var/list/negative_outer = make_two_dimensions(-1, 2)
	var/list/negative_inner = make_two_dimensions(2, -1)
	var/list/zero_then_negative = make_two_dimensions(0, -1)
	var/list/fractional = make_two_dimensions(1.5, 2.5)
	var/list/null_inner = make_two_dimensions(2, null)
	var/list/text_inner = make_two_dimensions(2, "2")
	var/list/datum_inner = make_two_dimensions(2, new /datum)
	var/list/list_inner = make_two_dimensions(2, list(2))
	ASSERT(negative_outer.len == 0)
	ASSERT(negative_inner.len == 2 && negative_inner[1].len == 0)
	ASSERT(zero_then_negative.len == 0)
	ASSERT(fractional.len == 1 && fractional[1].len == 2)
	ASSERT(null_inner.len == 2 && null_inner[1].len == 0)
	ASSERT(text_inner.len == 2 && text_inner[1].len == 0)
	ASSERT(datum_inner.len == 2 && datum_inner[1].len == 0)
	ASSERT(list_inner.len == 2 && list_inner[1].len == 0)

	var/list/constructed_negative = construct_one_dimension(-1)
	var/list/constructed_negative_fraction = construct_one_dimension(-0.5)
	var/list/constructed_negative_outer = construct_two_dimensions(-1, 2)
	var/list/constructed_negative_inner = construct_two_dimensions(2, -1)
	var/list/constructed_zero_then_negative = construct_two_dimensions(0, -1)
	ASSERT(constructed_negative.len == 0)
	ASSERT(constructed_negative_fraction.len == 0)
	ASSERT(constructed_negative_outer.len == 0)
	ASSERT(constructed_negative_inner.len == 2 && constructed_negative_inner[1].len == 0)
	ASSERT(constructed_zero_then_negative.len == 0)

	var/list/evaluated[first_dimension()][second_dimension()]
	ASSERT(dimension_events == 12)
	ASSERT(first_dimension_calls == 1)
	ASSERT(second_dimension_calls == 1)
	ASSERT(evaluated.len == 2 && evaluated[1].len == 3)
