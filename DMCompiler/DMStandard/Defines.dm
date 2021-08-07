#define TRUE 1
#define FALSE 0

#define NORTH 1
#define SOUTH 2
#define EAST 4
#define WEST 8
#define UP 16
#define DOWN 32
#define NORTHEAST 5 // NORTH | EAST
#define SOUTHEAST 6 // SOUTH | EAST
#define SOUTHWEST 10 // SOUTH | WEST
#define NORTHWEST 9 // NORTH | WEST

#define FLOAT_LAYER -1
#define AREA_LAYER 1
#define TURF_LAYER 2
#define OBJ_LAYER 3
#define MOB_LAYER 4
#define FLY_LAYER 5
#define BACKGROUND_LAYER 20000

#define FLOAT_PLANE -32767

#define FEMALE "female"
#define MALE "male"
#define NEUTER "neuter"
#define PLURAL "plural"

#define ANIMATION_END_NOW 1
#define ANIMATION_LINEAR_TRANSFORM 2
#define ANIMATION_PARALLEL 4
#define EASE_IN 64
#define EASE_OUT 128
#define ANIMATION_RELATIVE 256

#define NO_STEPS 0
#define FORWARD_STEPS 1
#define SLIDE_STEPS 2
#define SYNC_STEPS 3

//world.system_type
#define UNIX 0
#define MS_WINDOWS 1

//Icon blending functions
#define ICON_ADD 0
#define ICON_SUBTRACT 1
#define ICON_MULTIPLY 2
#define ICON_OVERLAY 3
#define ICON_AND 4
#define ICON_OR 5
#define ICON_UNDERLAY 6

//mob.sight
#define SEE_INFRA		(1<<0) // can see infra-red objects
#define SEE_SELF		(1<<1) // can see self, no matter what
#define SEE_MOBS		(1<<2) // can see all mobs, no matter what
#define SEE_OBJS		(1<<3) // can see all objs, no matter what
#define SEE_TURFS		(1<<4) // can see all turfs (and areas), no matter what
#define SEE_PIXEL		(1<<5) // if an object is located on an unlit area, but some of its pixels are in a lit area (via pixel_x,y or smooth movement), can see those pixels
#define SEE_THRU		(1<<6) // can see through opaque objects
#define SEE_BLACKNESS	(1<<7) // render dark tiles as blackness
#define BLIND			(1<<8) // can't see anything

//client.perspective
#define MOB_PERSPECTIVE 0
#define EYE_PERSPECTIVE 1
#define EDGE_PERSPECTIVE 2

//regex
#define REGEX_QUOTE(a) regex((a), 1)
#define REGEX_QUOTE_REPLACEMENT(a) regex((a), 2)
