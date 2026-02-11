Shader "Custom/StencilOverlay"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Pass
        {
            // Render the overlay
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGBA

            // Use stencil buffer
            Stencil
            {
                Ref 1           // Reference value
                Comp NotEqual   // Render where stencil is NOT 1
                Pass Keep       // Keep the existing stencil value
            }
        }
    }
}