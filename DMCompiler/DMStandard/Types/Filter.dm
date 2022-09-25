
/filter
    var/type

/filter/alpha
    type = "alpha"
    var/x
    var/y 
    var/icon
    var/render_source
    var/flags

/filter/angular_blur
    type = "angular_blur"
    var/x
    var/y 
    var/size

/filter/bloom
    type = "bloom"
    var/threshold
    var/size
    var/offset
    var/alpha

/filter/blur //gaussian blur
    type = "blur"
    var/size

/filter/color
    type = "color"
    var/color
    var/space

/filter/displace
    type = "displace"
    var/x
    var/y
    var/size
    var/icon
    var/render_source

/filter/drop_shadow
    type = "drop_shadow"
    var/x
    var/y
    var/size
    var/offset
    var/color

/filter/layer
    type = "layer"
    var/x
    var/y
    var/icon
    var/render_source
    var/flags
    var/color
    var/transform
    var/blend_mode
        
/filter/motion_blur
    type = "motion_blur"
    var/x        
    var/y

/filter/outline
    type = "outline"
    var/size
    var/color
    var/flags

/filter/radial_blur
    type = "radial_blur"
    var/x 
    var/y
    var/size

/filter/rays
    type = "rays"
    var/x
    var/y
    var/size
    var/color
    var/offset
    var/density
    var/threshold
    var/factor
    var/flags

/filter/ripple
    type = "ripple"
    var/x
    var/y
    var/size
    var/repeat
    var/radius
    var/falloff
    var/flags

/filter/wave
    type = "wave"
    var/x
    var/y
    var/size
    var/offset
    var/flags
        
/filter/greyscale //OD exclusive filter, definitely not just for debugging
    type = "greyscale"