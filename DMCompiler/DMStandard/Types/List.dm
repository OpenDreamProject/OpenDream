/list
	var/len

	proc/New(Size)
		if (Size != null) len = Size

	proc/Add(Item1)
	proc/Copy(Start = 1, End = 0)
	proc/Cut(Start = 1, End = 0)
	proc/Find(Elem, Start = 1, End = 0)
	proc/Insert(Index, Item1)
	proc/Remove(Item1)
	proc/Swap(Index1, Index2)

	proc/Join(Glue, Start = 1, End = 0)
		if (End == 0) End = src.len

		var/result = ""
		for (var/i in Start to End)
			result += "[src[i]][(i != End) ? Glue : ""]"

		return result