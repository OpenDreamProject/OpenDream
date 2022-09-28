using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;

namespace OpenDreamShared.Dream {
    [Serializable, NetSerializable]
    public sealed class DreamFilter : IEquatable<DreamFilter> {
        public bool used = false; //this may not be the best way of doing this
        [ViewVariables] public string? filter_type = null;
        [ViewVariables] public float? filter_x = null;
        [ViewVariables] public float? filter_y = null;
        [ViewVariables] public Object? filter_icon = null; //todo what is this?
        [ViewVariables] public Object? filter_render_source = null; //todo what is this?
        [ViewVariables] public float? filter_flags = null;
        [ViewVariables] public float? filter_size = null;
        [ViewVariables] public string? filter_color_string = null;
        [ViewVariables] public string? filter_threshold_color = null;
        [ViewVariables] public float? filter_threshold_strength = null;
        [ViewVariables] public float? filter_offset = null;
        [ViewVariables] public float? filter_alpha = null;
        [ViewVariables] public Object? filter_color_matrix = null; //matrix type?
        [ViewVariables] public float? filter_space = null;
        [ViewVariables] public Object? filter_transform = null; //matrix type?
        [ViewVariables] public float? filter_blend_mode = null;
        [ViewVariables] public float? filter_density = null;
        [ViewVariables] public float? filter_factor = null;
        [ViewVariables] public float? filter_repeat = null;
        [ViewVariables] public float? filter_radius = null;
        [ViewVariables] public float? filter_falloff = null;
        public DreamFilter() { }

        public override bool Equals(object obj) => obj is DreamFilter filter && Equals(filter);

        public bool Equals(DreamFilter filter) {
            if(filter.filter_type != filter_type) return false;
            if(filter.filter_x != filter_x) return false;
            if(filter.filter_y != filter_y) return false;
            if(filter.filter_icon != filter_icon) return false;
            if(filter.filter_render_source != filter_render_source) return false;
            if(filter.filter_flags != filter_flags) return false;
            if(filter.filter_size != filter_size) return false;
            if(filter.filter_color_string != filter_color_string) return false;
            if(filter.filter_threshold_color != filter_threshold_color) return false;
            if(filter.filter_threshold_strength != filter_threshold_strength) return false;
            if(filter.filter_offset != filter_offset) return false;
            if(filter.filter_alpha != filter_alpha) return false;
            if(filter.filter_color_matrix != filter_color_matrix) return false;
            if(filter.filter_space != filter_space) return false;
            if(filter.filter_transform != filter_transform) return false;
            if(filter.filter_blend_mode != filter_blend_mode) return false;
            if(filter.filter_density != filter_density) return false;
            if(filter.filter_factor != filter_factor) return false;
            if(filter.filter_repeat != filter_repeat) return false;
            if(filter.filter_radius != filter_radius) return false;
            if(filter.filter_falloff != filter_falloff) return false;
            return true;
        }

        public override int GetHashCode() {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            string result = "[";
            if(filter_type != null) result += $"type: {filter_type}, ";
            if(filter_x != null) result += $"x: {filter_x}, ";
            if(filter_y != null) result += $"y: {filter_y}, ";
            if(filter_icon != null) result += $"icon: {filter_icon}, ";
            if(filter_render_source != null) result += $"render_source: {filter_render_source}, ";
            if(filter_flags != null) result += $"flags: {filter_flags}, ";
            if(filter_size != null) result += $"size: {filter_size}, ";
            if(filter_color_string != null) result += $"color_string: {filter_color_string}, ";
            if(filter_threshold_color != null) result += $"threshold_color: {filter_threshold_color}, ";
            if(filter_threshold_strength != null) result += $"threshold_strength: {filter_threshold_strength}, ";
            if(filter_offset != null) result += $"offset: {filter_offset}, ";
            if(filter_alpha != null) result += $"alpha: {filter_alpha}, ";
            if(filter_color_matrix != null) result += $"color_matrix: {filter_color_matrix}, ";
            if(filter_space != null) result += $"space: {filter_space}, ";
            if(filter_transform != null) result += $"transform: {filter_transform}, ";
            if(filter_blend_mode != null) result += $"blend_mode: {filter_blend_mode}, ";
            if(filter_density != null) result += $"density: {filter_density}, ";
            if(filter_factor != null) result += $"factor: {filter_factor}, ";
            if(filter_repeat != null) result += $"repeat: {filter_repeat}, ";
            if(filter_radius != null) result += $"radius: {filter_radius}, ";
            if(filter_falloff != null) result += $"falloff: {filter_falloff}, ";
            result += "]";
            return result;
        }


    }


}
