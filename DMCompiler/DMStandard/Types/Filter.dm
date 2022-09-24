
/filter
    var/const/type

/filter/alpha
    var/x
    var/y 
    var/icon
    var/render_source
    var/flags

/filter/angular_blur
    var/x
    var/y 
    var/size

/filter/bloom
    var/threshold
    var/size
    var/offset
    var/alpha

/filter/blur //gaussian blur
    var/size

/filter/color
    var/color
    var/space

/filter/displace
    var/x
    var/y
    var/size
    var/icon
    var/render_source

/filter/drop_shadow
    var/x
    var/y
    var/size
    var/offset
    var/color

/filter/layer
    var/x
    var/y
    var/icon
    var/render_source
    var/flags
    var/color
    var/transform
    var/blend_mode
        
/filter/motion_blur
    var/x        
    var/y

/filter/outline
    type = "outline"
    var/size
    var/color
    var/flags

/filter/radial_blur
    var/x 
    var/y
    var/size

/filter/rays
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
    var/x
    var/y
    var/size
    var/repeat
    var/radius
    var/falloff
    var/flags

/filter/wave
    var/x
    var/y
    var/size
    var/offset
    var/flags
        
/filter/greyscale //OD exclusive filter, definitely not just for debugging
    type = "greyscale"