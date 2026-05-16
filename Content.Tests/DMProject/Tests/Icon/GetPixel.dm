/proc/RunTest()
	var/icon/I = icon('hanoi.dmi')
	ASSERT(I.GetPixel(5,5) == "#ff0000")
	var/list/targetx_pixels = list("#e8083f", "#ff0000", "#6aee1d", "#f1b004", "#ffa308", "#ff34e2", "#6aeeff",\
			"#f4f14d", "#fbed01", "#757121", "#c07760", "#cc7d66", "#c98164", "#5fed26", "#43fc13", "#43fc13", "#43fc13",\
			"#43fc13",	"#43fc13", "#48fa16", "#bf885e", "#625230", "#4d4b26", "#4c5234", "#09f5f3", "#00ffff", "#00ffff","#f44dfb",\
			"#2c14ff", "#001bf9", "#00ef58", "#f74d04")
	var/list/targety_pixels = list("#e8083f","#ff0000","#6bee1a","#1452f2","#e00bfc","#5be5fe","#00ffff","#f2ef4d",\
			"#6b6622","#4b4b25","#c07760","#cb8065","#57f020","#43fc13","#43fc13","#43fc13","#43fc13","#43fc13",\
			"#42fc12","#c68c63","#c87c64","#625230","#4c4a26","#f5e921","#2ffef1","#00f9ff","#ee54fc","#ffa04b",\
			"#f8a225","#3530e4","#00ef58","#f74d04")
	for(var/x in 1 to 32)
		ASSERT(I.GetPixel(x,16,"target") == targetx_pixels[x])
	for(var/y in 1 to 32)
		ASSERT(I.GetPixel(16,y,"target") == targety_pixels[y])	

	//chose by fair dice roll, guaranteed to be random
	var/list/random_pixels = list(\
		list(9, 2, "#ff0000"),\
		list(31, 8, "#00ef58"),\
		list(32, 32, "#fe1100"),\
		list(1, 1, "#d3105d"),\
		list(2, 13, "#ff0000"),\
		list(0, 0, null),\
		list(31, 31, "#00fe28"),\
		list(-1, 17, null),\
	)	
	for(var/list/tuple in random_pixels)
		ASSERT(I.GetPixel(tuple[1], tuple[2], "target") == tuple[3])

//byond bugs:
//- invalid iconstate causes error.log to be created but it's empty and no runtime triggers
//- invalid dir value (ie, 100) causes a hard crash