#ifndef EVO_BLOCKS_INPUT
#define EVO_BLOCKS_INPUT


StructuredBuffer<float4x4> _Matrix;



// ç¿ïWì¡íË
float3 MatrixPosition3(uint SV_IID, float4 Vertex)
{
    return mul(_Matrix[SV_IID], Vertex);
}

#endif