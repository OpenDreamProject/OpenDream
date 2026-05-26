/proc/RunTest()
	// ICON_ADD
	var/icon/B = icon('hanoi.dmi',"gradient")
	B.Blend(rgb(200, 10, 0, 170), ICON_ADD)
	var/list/target_pixels = list("#c80affaa","#c80affaa","#c80affa9","#c80aff9f","#c80aff95","#c80aff8c","#c80aff83","#c80aff7b","#c80aff73","#c80aff6c","#c80aff65","#c80aff5f","#c80aff58","#c80aff52","#c80aff4d","#c80aff47","#c80aff42","#c80aff3d","#c80aff39","#c80aff34","#c80aff30","#c80aff2c","#c80aff28","#c80aff25","#c80aff21","#c80aff1f","#c80aff1b","#c80aff19","#c80aff16","#c80aff14","#c80aff11","#c80aff0f")
	var/list/actual_pixels = list()
	var/matching = TRUE
	for(var/x in 1 to 32)
		matching &= (B.GetPixel(x,x) == target_pixels[x])
		actual_pixels += B.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_ADD did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_SUBTRACT
	B = icon('hanoi.dmi',"gradient")
	B.Blend(rgb(200,10,0,170), ICON_SUBTRACT)
	target_pixels = list("#0000ffaa","#0000ffaa","#0000ffa9","#0000ff9f","#0000ff95","#0000ff8c","#0000ff83","#0000ff7b","#0000ff73","#0000ff6c","#0000ff65","#0000ff5f","#0000ff58","#0000ff52","#0000ff4d","#0000ff47","#0000ff42","#0000ff3d","#0000ff39","#0000ff34","#0000ff30","#0000ff2c","#0000ff28","#0000ff25","#0000ff21","#0000ff1f","#0000ff1b","#0000ff19","#0000ff16","#0000ff14","#0000ff11","#0000ff0f")
	actual_pixels = list()
	matching = TRUE
	for(var/x in 1 to 32)
		matching &= (B.GetPixel(x,x) == target_pixels[x])
		actual_pixels += B.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_SUBTRACT did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_MULTIPLY
	B = icon('hanoi.dmi',"gradient")
	B.Blend(rgb(200,10,0,170), ICON_MULTIPLY)
	target_pixels = list("#000000aa","#000000aa","#000000a9","#0000009f","#00000095","#0000008c","#00000083","#0000007b","#00000073","#0000006c","#00000065","#0000005f","#00000058","#00000052","#0000004d","#00000047","#00000042","#0000003d","#00000039","#00000034","#00000030","#0000002c","#00000028","#00000025","#00000021","#0000001f","#0000001b","#00000019","#00000016","#00000014","#00000011","#0000000f")
	actual_pixels = list()
	matching = TRUE
	for(var/x in 1 to 32)
		matching &= (B.GetPixel(x,x) == target_pixels[x])
		actual_pixels += B.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_MULTIPLY did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_OVERLAY
	B = icon('hanoi.dmi',"gradient")
	B.Blend(rgb(200,10,0,170), ICON_OVERLAY)
	target_pixels = list("#850755","#850755","#850755","#850755f9","#850755f5","#850755f0","#850755ec","#850755e8","#850755e4","#850755e0","#850755dc","#850755d9","#850755d6","#850755d3","#850755d0","#850755cd","#850755cb","#850755c9","#850755c6","#850755c4","#850755c2","#850755c0","#850755be","#850755bc","#850755bb","#850755b9","#850755b8","#850755b6","#850755b5","#850755b4","#850755b3","#850755b2")
	actual_pixels = list()
	for(var/x in 1 to 32)
		matching &= (B.GetPixel(x,x) == target_pixels[x])
		actual_pixels += B.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_OVERLAY did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_AND
	B = icon('hanoi.dmi',"gradient")
	B.Blend(rgb(200,10,0,170), ICON_AND)
	target_pixels = list("#c80affaa","#c80affaa","#c80affa9","#c80aff9f","#c80aff95","#c80aff8c","#c80aff83","#c80aff7b","#c80aff73","#c80aff6c","#c80aff65","#c80aff5f","#c80aff58","#c80aff52","#c80aff4d","#c80aff47","#c80aff42","#c80aff3d","#c80aff39","#c80aff34","#c80aff30","#c80aff2c","#c80aff28","#c80aff25","#c80aff21","#c80aff1f","#c80aff1b","#c80aff19","#c80aff16","#c80aff14","#c80aff11","#c80aff0f")
	actual_pixels = list()
	matching = TRUE
	for(var/x in 1 to 32)
		matching &= (B.GetPixel(x,x) == target_pixels[x])
		actual_pixels += B.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_AND did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_OR
	B = icon('hanoi.dmi',"gradient")
	B.Blend(rgb(200,10,0,170), ICON_OR)
	target_pixels = list("#c80aff","#c80aff","#c80aff","#c80afff9","#c80afff5","#c80afff0","#c80affec","#c80affe8","#c80affe4","#c80affe0","#c80affdc","#c80affd9","#c80affd6","#c80affd3","#c80affd0","#c80affcd","#c80affcb","#c80affc9","#c80affc6","#c80affc4","#c80affc2","#c80affc0","#c80affbe","#c80affbc","#c80affbb","#c80affb9","#c80affb8","#c80affb6","#c80affb5","#c80affb4","#c80affb3","#c80affb2")
	actual_pixels = list()
	matching = TRUE
	for(var/x in 1 to 32)
		matching &= (B.GetPixel(x,x) == target_pixels[x])
		actual_pixels += B.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_OR did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")

	// ICON_UNDERLAY 
	B = icon('hanoi.dmi',"gradient")
	B.Blend(rgb(200,10,0,170), ICON_UNDERLAY)
	target_pixels = list("#0000ff","#0000ff","#0100fe","#0d01eef9","#1801e0f5","#2302d2f0","#2d02c5ec","#3703b9e8","#4003ade4","#4904a2e0","#520497dc","#59048ed9","#600584d6","#68057bd3","#6e0573d0","#75066acd","#7a0663cb","#80065cc9","#850755c6","#8b074ec4","#900748c2","#940742c0","#99083cbe","#9d0837bc","#a10832bb","#a4082eb9","#a80829b8","#ab0925b6","#ae0921b5","#b0091eb4","#b4091ab3","#b60917b2")
	actual_pixels = list()
	matching = TRUE
	for(var/x in 1 to 32)
		matching &= (B.GetPixel(x,x) == target_pixels[x])
		actual_pixels += B.GetPixel(x,x)
	if(!matching)
		CRASH("ICON_UNDERLAY did not match, expected [json_encode(target_pixels)] but got [json_encode(actual_pixels)]")
