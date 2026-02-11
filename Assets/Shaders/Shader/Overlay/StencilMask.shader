Shader "Custom/StencilMask"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Pass
        {
            // Do not render to color/depth buffer
            ColorMask 0 
            ZWrite Off

            // Write to the stencil buffer
            Stencil
            {
                Ref 1           // Reference value
                Comp Always     // Always write
                Pass Replace    // Replace stencil value with Ref
            }
        }
    }
}