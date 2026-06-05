/proc/blend_test(var/operation, var/list/target_pixels)
	var/icon/A = icon('hanoi.dmi',"reddot")
	var/icon/B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, operation)
	var/list/actual_pixels = list()
	var/matching = TRUE
	for(var/x in 1 to 32)
		matching &= (A.GetPixel(x,x) == target_pixels[x])
		actual_pixels += A.GetPixel(x,x)
	if(!matching)
		var/operation_friendly = "INVALID!"
		switch(operation)
			if(ICON_ADD)
				operation_friendly = "ICON_ADD"
			if(ICON_SUBTRACT)
				operation_friendly = "ICON_SUBTRACT"
			if(ICON_MULTIPLY)
				operation_friendly = "ICON_MULTIPLY"
			if(ICON_OVERLAY)
				operation_friendly = "ICON_OVERLAY"
			if(ICON_AND)
				operation_friendly = "ICON_AND"
			if(ICON_OR)
				operation_friendly = "ICON_OR"
			if(ICON_UNDERLAY)
				operation_friendly = "ICON_UNDERLAY"

		CRASH("[operation_friendly] did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

/proc/RunTest()
	// ICON_ADD
	var/list/target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null)
	blend_test(ICON_ADD, target_pixels)

	// ICON_SUBTRACT
	target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#741900","#741900","#741900","#741900",null,null,null,null,"#ff1900","#ff1900","#ff1900","#ff1900",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#741900","#741900","#741900","#741900",null,null,null,null,"#ff1900","#ff1900","#ff1900","#ff1900",null,null,null,null,null,null,null,null,null,null)
	blend_test(ICON_SUBTRACT, target_pixels)

	// ICON_MULTIPLY
	target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#8b0035","#8b0035","#8b0035","#8b0035",null,null,null,null,"#000035","#000035","#000035","#000035",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#8b0035","#8b0035","#8b0035","#8b0035",null,null,null,null,"#000035","#000035","#000035","#000035",null,null,null,null,null,null,null,null,null,null)
	blend_test(ICON_MULTIPLY, target_pixels)

	// ICON_OVERLAY
	target_pixels = list(null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null)
	blend_test(ICON_OVERLAY, target_pixels)

	// ICON_AND
	target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null)
	blend_test(ICON_AND, target_pixels)

	// ICON_OR
	target_pixels = list(null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null,null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null)
	blend_test(ICON_OR, target_pixels)

	// ICON_UNDERLAY 
	target_pixels = list(null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null,null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null)
	blend_test(ICON_UNDERLAY, target_pixels)
