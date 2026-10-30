/obj/stringify
    name = "object"

/obj/stringify/proper
    name = "\proper object"

/obj/stringify/oper/proc/operator""()
	// NOTE: different vowel-start
    return "stringified object"

/obj/stringify/oper/proper/operator""()
    return "\proper stringified object"

/datum/stringify
    var/name = "datum"

/datum/stringify/proper
    name = "\proper datum"

/datum/stringify/oper/proc/operator""()
	// NOTE: different vowel-start
    return "operated datum"

/datum/stringify/oper/proper/operator""()
    return "\proper operated datum"

/proc/RunTest()
	// Number
	ASSERT("[1]" == "1")
	ASSERT("\The [1]" == "")
	ASSERT("\the [1]" == "")
	ASSERT("It's [1]." == "It's 1.")
	ASSERT("\a [1]" == "")

	// Path
	ASSERT("[/obj/stringify]" == "/obj/stringify")
	ASSERT("\The [/obj/stringify]" == "/obj/stringify")
	ASSERT("\the [/obj/stringify]" == "/obj/stringify")
	ASSERT("It's [/obj/stringify]." == "It's /obj/stringify.")
	ASSERT("\a [/obj/stringify]" == "/obj/stringify")

	// Atom
	// TODO ASSERT("[new /obj/stringify]" == "The object")
	ASSERT("\The [new /obj/stringify]" == "The object")
	ASSERT("\the [new /obj/stringify]" == "the object")
	ASSERT("It's [new /obj/stringify]." == "It's the object.")
	ASSERT("\a [new /obj/stringify]" == "an object")

	ASSERT("[new /obj/stringify/proper]" == "object")
	ASSERT("\The [new /obj/stringify/proper]" == "object")
	ASSERT("\the [new /obj/stringify/proper]" == "object")
	ASSERT("It's [new /obj/stringify/proper]." == "It's object.")
	ASSERT("\a [new /obj/stringify/proper]" == "object")

	// TODO ASSERT("[new /obj/stringify/oper]" == "The stringified object")
	ASSERT("\The [new /obj/stringify/oper]" == "The stringified object")
	ASSERT("\the [new /obj/stringify/oper]" == "the stringified object")
	ASSERT("It's [new /obj/stringify/oper]." == "It's the stringified object.")
	ASSERT("\a [new /obj/stringify/oper]" == "a stringified object")

	ASSERT("[new /obj/stringify/oper/proper]" == "stringified object")
	ASSERT("\The [new /obj/stringify/oper/proper]" == "stringified object")
	ASSERT("\the [new /obj/stringify/oper/proper]" == "stringified object")
	ASSERT("It's [new /obj/stringify/oper/proper]." == "It's stringified object.")
	ASSERT("\a [new /obj/stringify/oper/proper]" == "stringified object")

	// Datum
	ASSERT("[new /datum/stringify]" == "datum")
	ASSERT("\The [new /datum/stringify]" == "The datum")
	ASSERT("\the [new /datum/stringify]" == "the datum")
	ASSERT("It's [new /datum/stringify]." == "It's datum.")
	ASSERT("\a [new /datum/stringify]" == "a datum")

	ASSERT("[new /datum/stringify/proper]" == "datum")
	ASSERT("\The [new /datum/stringify/proper]" == "datum")
	ASSERT("\the [new /datum/stringify/proper]" == "datum")
	ASSERT("It's [new /datum/stringify/proper]." == "It's datum.")
	ASSERT("\a [new /datum/stringify/proper]" == "datum")

	ASSERT("[new /datum/stringify/oper]" == "operated datum")
	ASSERT("\The [new /datum/stringify/oper]" == "The operated datum")
	ASSERT("\the [new /datum/stringify/oper]" == "the operated datum")
	ASSERT("It's [new /datum/stringify/oper]." == "It's operated datum.")
	ASSERT("\a [new /datum/stringify/oper]" == "an operated datum")

	ASSERT("[new /datum/stringify/oper/proper]" == "operated datum")
	ASSERT("\The [new /datum/stringify/oper/proper]" == "operated datum")
	ASSERT("\the [new /datum/stringify/oper/proper]" == "operated datum")
	ASSERT("It's [new /datum/stringify/oper/proper]." == "It's operated datum.")
	ASSERT("\a [new /datum/stringify/oper/proper]" == "operated datum")

	// String
	ASSERT("["string"]" == "string")
	ASSERT("\The ["string"]" == "The string")
	ASSERT("\the ["string"]" == "the string")
	ASSERT("It's ["string"]." == "It's string.")
	ASSERT("\a ["string"]" == "a string")

	// ASSERT("["\proper string"]" == "string")
	ASSERT("\The ["\proper string"]" == "string")
	ASSERT("\the ["\proper string"]" == "string")
	// ASSERT("It's ["\proper string"]." == "It's string.")
	ASSERT("\a ["\proper string"]" == "string")
