/proc/__operator_test_error(reason) {throw EXCEPTION(reason)}
#define SAFE_CRASH(reason) __operator_test_error(reason)

#define EXPECT_ARGS(num...) if(!(args.len in list(num))) {SAFE_CRASH("[type]'s [callee.name] received [args.len] args when expecting [list(num).Join(" or ")]")}
#define LOG_OP(str) return (str)
#define VALIDATE_OP(op_var, operation, expected)\
	do{ \
		var/__cache = op_var; \
		var/__op = ##operation; \
		op_var = __cache; \
		if(__op != expected){ \
			SAFE_CRASH("[op_var.type]'s [expected] instead emitted [__op]"); \
		} \
	} while(FALSE)

#define SENTINEL_VALUE "\"this string, which means it should not have returned anything user-defined\""

/operator
	var/id
	var/caught_result

	New(id)
		src.id = id

	proc/catch_result(me)
		. = caught_result
		caught_result = null

	#define COMPLEX_OP(kind) proc/operator##kind()
	#define SIMPLE_OP(kind) COMPLEX_OP(##kind) {EXPECT_ARGS(1); LOG_OP("[id][#kind][args[1]]")}
	#define SIMPLE_OP_SELF(kind) COMPLEX_OP(##kind) {EXPECT_ARGS(0); LOG_OP("[#kind][id]")}
	#define NEVER_OP(kind) COMPLEX_OP(##kind) {SAFE_CRASH("[type]'s [callee.name] was invoked when it's a NEVER_OP")}

/arithmetic
	parent_type = /operator

	SIMPLE_OP(+)
	COMPLEX_OP(-)
		EXPECT_ARGS(0, 1)
		if(args.len == 0) 
			LOG_OP("-[id]") 
		else
			LOG_OP("[id]-[args[1]]")
	SIMPLE_OP(*)
	SIMPLE_OP(/)
	SIMPLE_OP(%)
	SIMPLE_OP(%%)
	SIMPLE_OP(**)
	SIMPLE_OP(|)
	SIMPLE_OP(&)
	SIMPLE_OP(^)
	SIMPLE_OP_SELF(~)
	COMPLEX_OP(<<) // not output
		EXPECT_ARGS(1)
		caught_result = "[id]<<[args[1]]"
		LOG_OP(caught_result)
	COMPLEX_OP(>>) // not input
		EXPECT_ARGS(1)
		caught_result = "[id]>>[args[1]]"
		LOG_OP(caught_result)

/comparison
	parent_type = /operator

	// SIMPLE_OP(~=)
	// SIMPLE_OP(!~)
	SIMPLE_OP(>)
	SIMPLE_OP(>=)
	SIMPLE_OP(<)
	SIMPLE_OP(<=)
//   SIMPLE_OP(<=>) //<=> is not implemented

/mutator
	parent_type = /operator

	NEVER_OP(+); SIMPLE_OP(+=)
	NEVER_OP(-); SIMPLE_OP(-=)
	NEVER_OP(*); SIMPLE_OP(*=)
	NEVER_OP(/); SIMPLE_OP(/=)
	NEVER_OP(%); SIMPLE_OP(%=)
	NEVER_OP(%%); SIMPLE_OP(%%=)
	// **= doesn't exist but if it did it'd go here
	NEVER_OP(|); SIMPLE_OP(|=)
	NEVER_OP(&); SIMPLE_OP(&=)
	NEVER_OP(^); SIMPLE_OP(^=)
	NEVER_OP(<<); COMPLEX_OP(<<=)
		EXPECT_ARGS(1)
		caught_result = "[id]<<=[args[1]]"
		LOG_OP(caught_result)
	NEVER_OP(>>); COMPLEX_OP(>>=)
		EXPECT_ARGS(1)
		caught_result = "[id]>>=[args[1]]"
		LOG_OP(caught_result)
	SIMPLE_OP(:=)

/special
	parent_type = /operator

	COMPLEX_OP(<<) // output
		EXPECT_ARGS(1, 2, 3)
		if(args.len == 1)
			SAFE_CRASH("[type]'s [callee.name] tried to bitshift when it should be output")
		else if(args.len == 2)
			caught_result = "[id]<<output([args[1]])"
		else
			if(args[2] != src)
				SAFE_CRASH("[type]'s [callee.name] did not provide itself when outputting (received [args[2]])")
			caught_result = "[id]<<output([args[1]],[args[3]])"
		LOG_OP(SENTINEL_VALUE)

	COMPLEX_OP(>>) // input
		EXPECT_ARGS(1, 2)
		if(args.len == 1)
			SAFE_CRASH("[type]'s [callee.name] tried to bitshift when it should be input")
		else 
			if(args[1] != null)
				SAFE_CRASH("[type]'s [callee.name] did not have (null) as first argument for input (received [args[1]])")
			if(args[2] != src)
				SAFE_CRASH("[type]'s [callee.name] did not provide itself when inputting (received [args[2]])")
		
		LOG_OP("[id]>>")

	COMPLEX_OP([])
		EXPECT_ARGS(1)
		caught_result = "[id]\[[args[1]]\]"
		LOG_OP(caught_result)
	
	COMPLEX_OP([]=)
		EXPECT_ARGS(2)
		caught_result = "[id]\[[args[1]]\]=[args[2]]"
		LOG_OP(SENTINEL_VALUE)

	COMPLEX_OP(++)
		EXPECT_ARGS(0, 1)
		if(args.len == 0)
			LOG_OP("++[id]")
		else if(args[1] != 1)
			SAFE_CRASH("[callee.name] expects to receive exactly 1 for A++")
		LOG_OP("[id]++")

	COMPLEX_OP(--)
		EXPECT_ARGS(0, 1)
		if(args.len == 0)
			LOG_OP("--[id]")
		else if(args[1] != 1)
			SAFE_CRASH("[callee.name] expects to receive exactly 1 for A--")
		LOG_OP("[id]--")

	COMPLEX_OP(_turn)
		EXPECT_ARGS(1)
		LOG_OP("turn([id],[args[1]])")

	COMPLEX_OP("")
		EXPECT_ARGS(0)
		LOG_OP("\[[id]\]")

/proc/RunTest()
	do /* Arithmetic */
		var/arithmetic/operator = new("A")
		VALIDATE_OP(operator, operator + "B", "A+B")
		VALIDATE_OP(operator, operator - "B", "A-B")
		// VALIDATE_OP(operator, -operator, "-A")
		VALIDATE_OP(operator, operator * "B", "A*B")
		VALIDATE_OP(operator, operator / "B", "A/B")
		// VALIDATE_OP(operator, operator % "B", "A%B")
		// VALIDATE_OP(operator, operator %% "B", "A%%B")
		// VALIDATE_OP(operator, operator ** "B", "A**B")
		VALIDATE_OP(operator, operator | "B", "A|B")
		VALIDATE_OP(operator, operator & "B", "A&B")
		// VALIDATE_OP(operator, operator ^ "B", "A^B")
		VALIDATE_OP(operator, ~operator, "~A")
		// VALIDATE_OP(operator, operator << "B", "A<<B")
		// VALIDATE_OP(operator, operator >> "B", "A>>B")

	/* Assignments should fallback to the original proc */
		// VALIDATE_OP(operator, operator += "B", "A+B")
		// VALIDATE_OP(operator, operator -= "B", "A-B")
		VALIDATE_OP(operator, operator *= "B", "A*B")
		VALIDATE_OP(operator, operator /= "B", "A/B")
		// VALIDATE_OP(operator, operator %= "B", "A%B")
		// VALIDATE_OP(operator, operator %%= "B", "A%%B")
		// **=
		VALIDATE_OP(operator, operator |= "B", "A|B")
		VALIDATE_OP(operator, operator &= "B", "A&B")
		// VALIDATE_OP(operator, operator ^= "B", "A^B")
		// VALIDATE_OP(operator, operator.catch_result(operator <<= "B"), "A<<B")
		// VALIDATE_OP(operator, operator.catch_result(operator >>= "B"), "A>>B")
	while(FALSE)

	do /* Comparisons */
		var/comparison/operator = new("A")
		/* These two just have generally buggy behaviour in BYOND, not sure why
		// VALIDATE_OP(operator, operator ~= "B", "A~=B")
		// VALIDATE_OP(operator, operator !~ "B", "A!~B")
		*/
		// VALIDATE_OP(operator, operator > "B", "A>B")
		// VALIDATE_OP(operator, operator >= "B", "A>=B")
		// VALIDATE_OP(operator, operator < "B", "A<B")
		// VALIDATE_OP(operator, operator <= "B", "A<=B")
		// VALIDATE_OP(operator, operator <=> "B", "A<=>B")
	while(FALSE)

	do /* Mutators */
		var/mutator/operator = new("A")
		// VALIDATE_OP(operator, operator += "B", "A+=B")
		// VALIDATE_OP(operator, operator -= "B", "A-=B")
		VALIDATE_OP(operator, operator *= "B", "A*=B")
		VALIDATE_OP(operator, operator /= "B", "A/=B")
		// VALIDATE_OP(operator, operator %= "B", "A%=B")
		// VALIDATE_OP(operator, operator %%= "B", "A%%=B")
		VALIDATE_OP(operator, operator |= "B", "A|=B")
		VALIDATE_OP(operator, operator &= "B", "A&=B")
		// VALIDATE_OP(operator, operator ^= "B", "A^=B")
		// VALIDATE_OP(operator, operator <<= "B", operator) // bitshift operators always return src
		// VALIDATE_OP(operator, operator.catch_result(operator), "A<<=B")
		// VALIDATE_OP(operator, operator >>= "B", operator)
		// VALIDATE_OP(operator, operator.catch_result(operator), "A>>=B")
		// VALIDATE_OP(operator, operator := "B", "A:=B")
	while(FALSE)

	do
		var/special/operator = new("A")
		/* List operators */
		VALIDATE_OP(operator, operator["idx"], "A\[idx\]")
		var/list_assign_result = (operator["idx"] = "B")
		if(list_assign_result != "B")
			SAFE_CRASH("[@"A[idx]=B"] returned a value other than B ([list_assign_result])")
		VALIDATE_OP(operator, operator.catch_result(operator), @"A[idx]=B")

		/* Output and Input */
		// operator << "B"
		// VALIDATE_OP(operator, operator.catch_result(operator), "A<<output(B)")
		// operator << output("B")
		// VALIDATE_OP(operator, operator.catch_result(operator), "A<<output(B)")
		// operator << output("B", null)
		// VALIDATE_OP(operator, operator.catch_result(operator), "A<<output(B)")
		// operator << output("B", "window")
		// VALIDATE_OP(operator, operator.catch_result(operator), "A<<output(B,window)")
		// operator << browse("B") // this appears to be a no-op
		// VALIDATE_OP(operator, operator.catch_result(operator), null)

		// var/input_var
		// operator >> input_var
		// VALIDATE_OP(operator, input_var, "A>>")
	
		/* Increment/Decrement */
		var/_op_cache = operator
		var/_ // container

		// Pre
		// VALIDATE_OP(_, ++operator, "++A")
		// VALIDATE_OP(operator, operator, "++A")
		operator = _op_cache
		// VALIDATE_OP(_, --operator, "--A")
		// VALIDATE_OP(operator, operator, "--A")
		operator = _op_cache

		// Post
		// VALIDATE_OP(_, operator++, _op_cache)
		// VALIDATE_OP(operator, operator, "A++")
		operator = _op_cache
		// VALIDATE_OP(_, operator--, _op_cache)
		// VALIDATE_OP(operator, operator, "A--")
		operator = _op_cache

		/* Misc */
		// VALIDATE_OP(operator, turn(operator, 90), "turn(A,B)") // BYOND bug
		VALIDATE_OP(operator, "[operator]", @"[A]")
	while(FALSE)