// COMPILE ERROR OD0012
#define TEST(a,b) a+b

/proc/main()
    world.log << TEST(1, 2+
2)