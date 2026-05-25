/proc/RunTest()
	// ICON_ADD
	var/icon/A = icon('hanoi.dmi',"reddot")
	var/icon/B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_ADD)
	var/list/target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null)
	var/list/actual_pixels = list()
	var/matching = TRUE
	for(var/x in 1 to 32)
		matching &= (A.GetPixel(x,x) == target_pixels[x])
		actual_pixels += A.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_ADD did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_SUBTRACT
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_SUBTRACT)
	target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#741900","#741900","#741900","#741900",null,null,null,null,"#ff1900","#ff1900","#ff1900","#ff1900",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#741900","#741900","#741900","#741900",null,null,null,null,"#ff1900","#ff1900","#ff1900","#ff1900",null,null,null,null,null,null,null,null,null,null)
	actual_pixels = list()
	matching = TRUE
	for(var/x in 1 to 32)
		matching &= (A.GetPixel(x,x) == target_pixels[x])
		actual_pixels += A.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_SUBTRACT did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_MULTIPLY
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_MULTIPLY)
	target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#8b0035","#8b0035","#8b0035","#8b0035",null,null,null,null,"#000035","#000035","#000035","#000035",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#8b0035","#8b0035","#8b0035","#8b0035",null,null,null,null,"#000035","#000035","#000035","#000035",null,null,null,null,null,null,null,null,null,null)
	actual_pixels = list()
	matching = TRUE
	for(var/x in 1 to 32)
		matching &= (A.GetPixel(x,x) == target_pixels[x])
		actual_pixels += A.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_MULTIPLY did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_OVERLAY
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_OVERLAY)
	target_pixels = list(null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null,null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null)
	for(var/x in 1 to 32)
		matching &= (A.GetPixel(x,x) == target_pixels[x])
		actual_pixels += A.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_OVERLAY did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_AND
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_AND)
	target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null)
	actual_pixels = list()
	matching = TRUE
	for(var/x in 1 to 32)
		matching &= (A.GetPixel(x,x) == target_pixels[x])
		actual_pixels += A.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_AND did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_OR
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_OR)
	target_pixels = list(null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null,null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null)
	actual_pixels = list()
	matching = TRUE
	for(var/x in 1 to 32)
		matching &= (A.GetPixel(x,x) == target_pixels[x])
		actual_pixels += A.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_OR did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_UNDERLAY 
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_UNDERLAY)
	target_pixels = list(null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null,null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null)
	actual_pixels = list()
	matching = TRUE
	for(var/x in 1 to 32)
		matching &= (A.GetPixel(x,x) == target_pixels[x])
		actual_pixels += A.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_UNDERLAY did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")
