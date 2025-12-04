using Dungeon;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;



/// <summary> Cubu情報を格納するクラス </summary>
public class DrawMeshData
{
    public Mesh mesh_;
    public Material material_;
    public List<CubeInfomation> cube_list_;



    public DrawMeshData()
    {
        mesh_ = new Mesh();
        cube_list_ = new List<CubeInfomation>();
    }
}





/// <summary> DrawMeshを使用しての描画クラス、設定から描画まで </summary>
public class ViewDrawMesh : MonoBehaviour
{
    /// <summary> Procedural用の行列 </summary>
    private List<NativeArray<Matrix4x4>> matrix_produral;

    /// <summary> バッファリスト </summary>
    private List<GraphicsBuffer> _graphicsBuffers;

    /// <summary> Procedural描画用のCubu情報が入ったリスト </summary>
    private List<DrawMeshData> produral_data;



    void Awake()
    {
        matrix_produral = new List<NativeArray<Matrix4x4>>();

        _graphicsBuffers = new List<GraphicsBuffer>();

        produral_data = new List<DrawMeshData>();
    }


    void Start()
    {
        AddCube(DungeonManager.Map);
        DrawProceduralSetup();
    }

    void Update()
    {
        int mat = 0;


        // Proceduralの描画
        foreach (var v in produral_data)
        {
            NativeArray<Matrix4x4> temp = matrix_produral[mat];
            Bounds BOUNDS = new Bounds(Vector3.zero, v.mesh_.bounds.size * 600);


            mat++;
            Graphics.DrawMeshInstancedProcedural(v.mesh_, 0, v.material_, BOUNDS, temp.Length);
        }
    }



    /// <summary> Proceduralを使用するためのCubu情報を登録する </summary>
    private void AddCube(DungeonMap map)
    {
        Dictionary<string, List<CubeInfomation>> cubu_produral = new Dictionary<string, List<CubeInfomation>>();
        ICube[,,] map_data = map.MapData;
        var inst = new GameObject[map_data.GetLength(0), map_data.GetLength(1), map_data.GetLength(2)];
        int i = 0;
        map.CubeInstance = inst;

        var terrainRoot = new GameObject("Terrain");
        terrainRoot.transform.parent = DungeonManager.Instance.transform;
        map.TerrainRoot = terrainRoot.transform;



        for (int x = 0; x < map_data.GetLength(0); x++)
            for (int y = 0; y < map_data.GetLength(1); y++)
                for (int z = 0; z < map_data.GetLength(2); z++)
                {
                    var cube = map_data[x, y, z].GetInfomation(map, x, y, z);



                    if (cube != null && cube.Prefab.CompareTag("GenerateTarget"))
                    {
                        var gameObject = Instantiate(cube.Prefab, cube.Position, cube.Rotation, terrainRoot.transform);
                        gameObject.transform.localScale = cube.Scale;
                        inst[x, y, z] = gameObject;
                        continue;
                    }

                    // Cubu情報が問題なければ追加
                    if (cube != null &&
                        cube.Prefab.transform.childCount >= 1)
                    {
                        for (int child = 0; child < cube.Prefab.transform.childCount; ++child)
                        {
                            var children = cube.Prefab.transform.GetChild(child);
                            var children_cube = cube.Clone();
                            var point = children_cube.Position;   // 原点



                            // 天井のオブジェクトをはじく
                            if (map_data[x, y, z].CubeType == CubeType.ORNAMENT &&
                                (map_data[x, y + 1, z].CubeType == CubeType.STANDARD || map_data[x, y + 1, z].CubeType == CubeType.VOID))
                            {
                                continue;
                            }


                            // 最初にY=0の場合のパターンを作る
                            children_cube.Rotation *= children.gameObject.transform.localRotation;

                            children_cube.Position.x += children.gameObject.transform.localPosition.x;
                            children_cube.Position.y -= children.gameObject.transform.localPosition.y;
                            children_cube.Position.z += children.gameObject.transform.localPosition.z;

                            // オブジェクトを回転
                            Vector3 toPoint = children_cube.Position - point;
                            Quaternion rot = Quaternion.AngleAxis(cube.Rotation.eulerAngles.y, Vector3.up);

                            // 座標確定
                            children_cube.Position = point + (rot * toPoint);


                            // 親の基準が1,1,1の場合は子を基準にする。
                            if (children_cube.Scale == Vector3.one)
                            {
                                children_cube.Scale = children.gameObject.transform.localScale;
                            }

                            if (children.gameObject.CompareTag("GenerateTarget"))
                            {
                                var gameObject = Instantiate(children.gameObject, children_cube.Position, children_cube.Rotation, terrainRoot.transform);
                                gameObject.transform.localScale = children_cube.Scale;
                                inst[x, y, z] = gameObject;
                            }

                            // 自作シェーダーのみ登録
                            if (children.TryGetComponent<MeshFilter>(out MeshFilter Mseh) == true &&
                                children.GetComponent<Renderer>().sharedMaterial.shader.name == "Custom/EvoRougeBlocks")
                            {
                                // 新しいメッシュを登録
                                if (cubu_produral.ContainsKey(Mseh.sharedMesh.name) == false)
                                {
                                    cubu_produral.Add(Mseh.sharedMesh.name, new List<CubeInfomation>());
                                }
                                cubu_produral[Mseh.sharedMesh.name].Add(children_cube);
                            }
                        }
                    }
                }

        // Listに変換
        foreach (var kvp in cubu_produral)
        {
            produral_data.Add(new DrawMeshData());

            foreach (var v in kvp.Value)
            {
                produral_data[i].cube_list_.Add(v);
            }

            i++;
        }

    }

    /// <summary> 登録したCubuの座標等を設定する </summary>
    private void DrawProceduralSetup()
    {
        int mat = 0;



        // produral初期化
        foreach (var v in produral_data)
        {
            matrix_produral.Add(new NativeArray<Matrix4x4>(v.cube_list_.Count, Allocator.Persistent));
            NativeArray<Matrix4x4> temp = matrix_produral[mat];



            for (int i = 0; i < temp.Length; ++i)
            {
                temp[i] = Matrix4x4.TRS(v.cube_list_[i].Position, v.cube_list_[i].Rotation, v.cube_list_[i].Scale);
            }
            mat++;
        }

        // バッファ、マテリアル等の登録
        for (int i = 0; i < matrix_produral.Count; ++i)
        {
            _graphicsBuffers.Add(new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                matrix_produral[i].Length,
                Marshal.SizeOf<Matrix4x4>()));

            _graphicsBuffers[i].SetData(matrix_produral[i]);

            // メッシュ、マテリアルの登録
            produral_data[i].material_ = Instantiate(produral_data[i].cube_list_[0].Prefab.transform.GetChild(0).GetComponent<Renderer>().sharedMaterial);
            produral_data[i].material_.SetBuffer("_Matrix", _graphicsBuffers[i]);
            produral_data[i].mesh_ = produral_data[i].cube_list_[0].Prefab.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh;
        }
    }



    private void OnDisable()
    {
        foreach (var mat in matrix_produral)
            mat.Dispose();

        foreach (var g in _graphicsBuffers)
            g.Dispose();
    }
}