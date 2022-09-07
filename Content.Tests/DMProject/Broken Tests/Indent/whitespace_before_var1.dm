/* Comment before var*//var/whitespace_test_comment = TRUE

/* Comment and space before var*/ /var/whitespace_test_comment_spaced = TRUE

/proc/RunTest()
	ASSERT(whitespace_test_comment)
	ASSERT(whitespace_test_comment_spaced)
