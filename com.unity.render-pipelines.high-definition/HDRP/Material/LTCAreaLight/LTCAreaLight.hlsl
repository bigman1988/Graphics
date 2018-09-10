// Area light textures
TEXTURE2D_ARRAY(_LtcData); // We pack all Ltc data inside one texture array to limit the number of resource used

#define LTC_GGX_MATRIX_INDEX 0 // RGBA
#define LTC_DISNEY_DIFFUSE_MATRIX_INDEX 1 // RGBA

#define LTC_LUT_SIZE   64
#define LTC_LUT_SCALE  ((LTC_LUT_SIZE - 1) * rcp(LTC_LUT_SIZE))
#define LTC_LUT_OFFSET (0.5 * rcp(LTC_LUT_SIZE))


// BMAYAUX (18/07/09) Additional BRDFs, tables are stored with new encoding (i.e. U=Perceptual roughness, V=sqrt( 1 - cos(N.V) ))
#define LTC_MATRIX_INDEX_GGX            0//2
#define LTC_MATRIX_INDEX_DISNEY         0//3
#define LTC_MATRIX_INDEX_COOK_TORRANCE  0//4
#define LTC_MATRIX_INDEX_CHARLIE        0//5
#define LTC_MATRIX_INDEX_WARD           0//6
#define LTC_MATRIX_INDEX_OREN_NAYAR     0//7



// BMAYAUX (18/07/04) Added sampling helpers

// Expects NdotV clamped in [0,1]
float2  LTCGetSamplingUV( float NdotV, float perceptualRoughness ) {
    float2  xy;
    #if 1
        xy.x = perceptualRoughness;
        xy.y = FastACosPos(NdotV) * INV_HALF_PI;    // Originally, texture was accessed via theta so we had to take the acos(N.V)
    #else
        xy.x = perceptualRoughness;
        xy.y = sqrt( 1 - NdotV );                   // Now, we use V = sqrt( 1 - cos(theta) ) which is kind of linear and only requires a single sqrt() instead of an expensive acos()
    #endif

// Original code
//    return LTC_LUT_OFFSET + LTC_LUT_SCALE * float2( perceptualRoughness, theta * INV_HALF_PI );

    xy *= (LTC_LUT_SIZE-1);     // 0 is pixel 0, 1 = last pixel in the table
    xy += 0.5;                  // Perfect pixel sampling starts at the center
    return xy / LTC_LUT_SIZE;   // Finally, return UVs in [0,1]
}

// Fetches the transposed M^-1 matrix need for runtime LTC estimate
float3x3    LTCSampleMatrix( float2 UV, uint BRDFIndex ) {
    // Note we load the matrix transpose (to avoid having to transpose it in shader)
    float3x3    invM = 0.0;
                invM._m22 = 1.0;
                invM._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD( _LtcData, s_linear_clamp_sampler, UV, BRDFIndex, 0 );

    return invM;
}
