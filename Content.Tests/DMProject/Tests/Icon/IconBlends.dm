/proc/RunTest()
	// ICON_ADD
	var/icon/A = icon('hanoi.dmi',"reddot")
	var/icon/B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_ADD)
	var/list/target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null)
	for(var/x in 1 to 32)
		ASSERT(A.GetPixel(x,x) == target_pixels[x])
	// 	target_pixels[x] = A.GetPixel(x,x)
	// world.log << "ADD [json_encode(target_pixels)]"

	// ICON_SUBTRACT
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_SUBTRACT)
	target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#741900","#741900","#741900","#741900",null,null,null,null,"#ff1900","#ff1900","#ff1900","#ff1900",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#741900","#741900","#741900","#741900",null,null,null,null,"#ff1900","#ff1900","#ff1900","#ff1900",null,null,null,null,null,null,null,null,null,null)
	for(var/x in 1 to 32)
		ASSERT(A.GetPixel(x,x) == target_pixels[x])
	// 	target_pixels[x] = A.GetPixel(x,x)
	// world.log << "SUB [json_encode(target_pixels)]"

	// ICON_MULTIPLY
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_MULTIPLY)
	target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#8b0035","#8b0035","#8b0035","#8b0035",null,null,null,null,"#000035","#000035","#000035","#000035",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#8b0035","#8b0035","#8b0035","#8b0035",null,null,null,null,"#000035","#000035","#000035","#000035",null,null,null,null,null,null,null,null,null,null)
	for(var/x in 1 to 32)
		ASSERT(A.GetPixel(x,x) == target_pixels[x])
	// 	target_pixels[x] = A.GetPixel(x,x)
	// world.log << "MULT [json_encode(target_pixels)]"

	// ICON_OVERLAY
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_OVERLAY)
	target_pixels = list(null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null,null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null)
	for(var/x in 1 to 32)
		ASSERT(A.GetPixel(x,x) == target_pixels[x])
	// 	target_pixels[x] = A.GetPixel(x,x)
	// world.log << "OVERLAY [json_encode(target_pixels)]"

	// ICON_AND
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_AND)
	target_pixels = list(null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,"#ff19ff","#ff19ff","#ff19ff","#ff19ff",null,null,null,null,null,null,null,null,null,null)
	for(var/x in 1 to 32)
		ASSERT(A.GetPixel(x,x) == target_pixels[x])
	// 	target_pixels[x] = A.GetPixel(x,x)
	// world.log << "AND [json_encode(target_pixels)]"

	// ICON_OR
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_OR)
	target_pixels = list(null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null,null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff19ff","#ff19ff","#ff19ff","#ff19ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null)
	for(var/x in 1 to 32)
		ASSERT(A.GetPixel(x,x) == target_pixels[x])
	// 	target_pixels[x] = A.GetPixel(x,x)
	// world.log << "OR [json_encode(target_pixels)]"

	// ICON_UNDERLAY 
	A = icon('hanoi.dmi',"reddot")
	B = icon('hanoi.dmi',"bluedot")
	A.Blend(B, ICON_UNDERLAY)
	target_pixels = list(null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null,null,null,null,"#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#8b00ff","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#ff1935","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff","#0000ff",null,null,null)
	for(var/x in 1 to 32)
		ASSERT(A.GetPixel(x,x) == target_pixels[x])
	// 	target_pixels[x] = A.GetPixel(x,x)
	// world.log << "UNDERLAY [json_encode(target_pixels)]"
