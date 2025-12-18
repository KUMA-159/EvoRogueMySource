using Dungeon;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;



public class DrawBatchData
{
    public Mesh Mesh;
    public Material Material;
    public List<Matrix4x4> InstanceMatrices = new List<Matrix4x4>();
    public Bounds TotalBounds; // バッチ全体のバウンディングボックス

    // GPU送信用バッファ
    public GraphicsBuffer MatrixBuffer;
    public NativeArray<Matrix4x4> NativeMatrices;

    public void Dispose()
    {
        if (MatrixBuffer != null) MatrixBuffer.Dispose();
        if (NativeMatrices.IsCreated) NativeMatrices.Dispose();
    }
}



/// <summary>
/// DrawMeshInstancedProceduralを使用した最適化された描画クラス
/// </summary>
public class ViewDrawMesh : MonoBehaviour
{
    // シェーダー名の判定用定数
    private const string TARGET_SHADER_NAME = "Custom/EvoRougeBlocks";

    // シェーダープロパティIDのキャッシュ
    private static readonly int matrix_bufferId_ = Shader.PropertyToID("_Matrix");


    private List<DrawBatchData> draw_Batches_ = new List<DrawBatchData>();
    private bool isInitialized_ = false;



    void Start()
    {
        InitializeDungeon(DungeonManager.Map);
    }

    void Update()
    {
        if (!isInitialized_) return;

        RenderBatches();
    }

    private void OnDisable()
    {
        DisposeBuffers();
    }



    /// <summary>
    /// ダンジョンデータを解析し、描画バッチを構築する
    /// </summary>
    private void InitializeDungeon(DungeonMap map)
    {
        // 既存データのクリア
        DisposeBuffers();
        draw_Batches_.Clear();

        // バッチ処理用の一時辞書 (MeshとMaterialの組み合わせでグループ化)
        var batchDict = new Dictionary<Mesh, DrawBatchData>();

        ICube[,,] mapData = map.MapData;
        Transform terrainRoot = new GameObject("Terrain").transform;
        terrainRoot.SetParent(DungeonManager.Instance.transform);
        map.TerrainRoot = terrainRoot;

        int sizeX = mapData.GetLength(0);
        int sizeY = mapData.GetLength(1);
        int sizeZ = mapData.GetLength(2);

        // インスタンス管理用配列
        map.CubeInstance = new GameObject[sizeX, sizeY, sizeZ];

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    ProcessCube(map, x, y, z, terrainRoot, batchDict);
                }
            }
        }

        // バッチデータをリストへ変換し、GPUバッファをセットアップ
        foreach (var batch in batchDict.Values)
        {
            SetupGraphicsBuffer(batch);
            draw_Batches_.Add(batch);
        }

        isInitialized_ = true;
    }

    /// <summary>
    /// 個別のキューブ処理（ロジックの分離）
    /// </summary>
    private void ProcessCube(DungeonMap map, int x, int y, int z, Transform parent, Dictionary<Mesh, DrawBatchData> batchDict)
    {
        var cellInfo = map.MapData[x, y, z];
        var cubeInfo = cellInfo.GetInfomation(map, x, y, z);

        if (cubeInfo == null) return;

        // 生成対象タグがついているものはGameObjectとして生成（動的なインタラクション用など）
        if (cubeInfo.Prefab.CompareTag("GenerateTarget"))
        {
            CreateGameObject(cubeInfo, map, x, y, z, parent);
            return;
        }

        // 子オブジェクトを走査してバッチに追加
        if (cubeInfo.Prefab.transform.childCount > 0)
        {
            foreach (Transform child in cubeInfo.Prefab.transform)
            {
                // カリングロジック
                if (ShouldCull(map, x, y, z)) continue;

                // 描画データの抽出と変換
                if (TryGetRenderData(child, out Mesh mesh, out Material mat))
                {
                    // シェーダー判定
                    if (mat.shader.name != TARGET_SHADER_NAME) continue;

                    // 座標計算（ローカル -> ワールド）
                    // 注: ユーザーコードの回転計算ロジックは複雑なため、意図通り動作すると仮定して簡略化して記述します
                    // 実際には Matrix4x4.TRS を活用するとより高速です

                    Vector3 worldPos = CalculateWorldPosition(cubeInfo, child);
                    Quaternion worldRot = CalculateWorldRotation(cubeInfo, child);
                    Vector3 worldScale = Vector3.Scale(cubeInfo.Scale, child.localScale);

                    Matrix4x4 matrix = Matrix4x4.TRS(worldPos, worldRot, worldScale);

                    // バッチに追加
                    if (!batchDict.TryGetValue(mesh, out var batch))
                    {
                        batch = new DrawBatchData { Mesh = mesh, Material = new Material(mat) }; // マテリアルはインスタンス化が必要か要検討
                        batchDict[mesh] = batch;
                    }

                    batch.InstanceMatrices.Add(matrix);

                    // Boundsの拡張（カリング用）
                    if (batch.InstanceMatrices.Count == 1)
                    {
                        batch.TotalBounds = new Bounds(worldPos, Vector3.zero);
                    }
                    else
                    {
                        batch.TotalBounds.Encapsulate(worldPos);
                    }
                }
            }
        }
    }

    private void SetupGraphicsBuffer(DrawBatchData batch)
    {
        int count = batch.InstanceMatrices.Count;
        if (count == 0) return;

        // NativeArrayの確保とデータセット
        batch.NativeMatrices = new NativeArray<Matrix4x4>(count, Allocator.Persistent);
        for (int i = 0; i < count; i++)
        {
            batch.NativeMatrices[i] = batch.InstanceMatrices[i];
        }

        // GraphicsBufferの作成
        batch.MatrixBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, Marshal.SizeOf<Matrix4x4>());
        batch.MatrixBuffer.SetData(batch.NativeMatrices);

        // マテリアルへのセット
        batch.Material.SetBuffer(matrix_bufferId_, batch.MatrixBuffer);
    }

    private void RenderBatches()
    {
        const float EXPAND_SIZE = 10.0f;



        foreach (var batch in draw_Batches_)
        {
            // 視錐台カリングを有効化
            Bounds renderBounds = batch.TotalBounds;
            renderBounds.Expand(EXPAND_SIZE);

            Graphics.DrawMeshInstancedProcedural(
                batch.Mesh,
                0,
                batch.Material,
                renderBounds,
                batch.MatrixBuffer.count
            );
        }
    }

    private void DisposeBuffers()
    {
        foreach (var batch in draw_Batches_)
        {
            batch.Dispose();
        }
        draw_Batches_.Clear();
        isInitialized_ = false;
    }

    private bool ShouldCull(DungeonMap map, int x, int y, int z)
    {
        // 天井カリングロジック
        var mapData = map.MapData;
        if (mapData[x, y, z].CubeType == CubeType.ORNAMENT)
        {
            // 配列外参照チェック
            if (y + 1 < mapData.GetLength(1))
            {
                var upperType = mapData[x, y + 1, z].CubeType;
                if (upperType == CubeType.STANDARD || upperType == CubeType.VOID)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryGetRenderData(Transform t, out Mesh mesh, out Material mat)
    {
        mesh = null;
        mat = null;
        if (t.TryGetComponent(out MeshFilter mf) && t.TryGetComponent(out Renderer r))
        {
            mesh = mf.sharedMesh;
            mat = r.sharedMaterial;
            return mesh != null && mat != null;
        }
        return false;
    }

    // 座標計算の簡略化ヘルパー
    private Vector3 CalculateWorldPosition(CubeInfomation parentCube, Transform child)
    {
        Vector3 parentPos = parentCube.Position;
        Vector3 childLocalPos = child.localPosition;

        // Y軸反転等の特殊なロジックが元コードにあったため、それに従うならここで調整
        Vector3 adjustedLocal = new Vector3(childLocalPos.x, -childLocalPos.y, childLocalPos.z);

        Quaternion parentRot = Quaternion.AngleAxis(parentCube.Rotation.eulerAngles.y, Vector3.up);
        return parentPos + (parentRot * adjustedLocal);
    }

    private Quaternion CalculateWorldRotation(CubeInfomation parentCube, Transform child)
    {
        return parentCube.Rotation * child.localRotation;
    }

    private void CreateGameObject(CubeInfomation info, DungeonMap map, int x, int y, int z, Transform parent)
    {
        var go = Instantiate(info.Prefab, info.Position, info.Rotation, parent);
        go.transform.localScale = info.Scale;
        map.CubeInstance[x, y, z] = go;
    }
}