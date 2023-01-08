
//# issue 513

/proc/RunTest()
	var/L = list()

	ASSERT( (TRUE ? L : ()) == L )
	ASSERT( (FALSE ? L : ()) == null )

	ASSERT( (TRUE ? () : L) == null )
	ASSERT( (FALSE ? () : L) == L )

	ASSERT( (TRUE ? () : ()) == null )
	ASSERT( (FALSE ? () : ()) == null )
