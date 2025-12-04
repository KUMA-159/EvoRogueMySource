/**********************************************************************/
//  説明 :
//      AI移動処理callの基底クラス
/**********************************************************************/

using Dungeon.Entities.Enemy;
using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Entities.AI
{
    public class AIMoveAction
    {
        /// <summary> スタック回数 </summary>
        private byte stack_count_;
        private Vector3Int prev_point_;
        private Vector3Int prev_point_2_;


        /// <summary> 敵エンティティ </summary>
        protected EnemyEntity entity_;
        /// <summary> スタートからゴールの最短経路 </summary>
        protected List<Vector3Int> to_goal_;
        /// <summary> ノード要素数 </summary>
        protected byte goal_index_;


        /// <summary> 敵の目的地 </summary>
        public Vector3Int Destination { get; protected set; }
        /// <summary> 敵の目的地方向の向き </summary>
        public Direction8 DestinationFace { get; protected set; }
        /// <summary> 敵のMoveType </summary>
        public MoveType MovementType { get; protected set; }
        /// <summary> AreaChangeステートに遷移する条件 </summary>
        public bool IsAreaChange { get; protected set; }
        /// <summary> 初期化条件 </summary>
        public bool InitEntity { get; private set; }


        /// <summary> 推定コストを取得 </summary>
        /// <param name="StratPoint"> スタート地点 </param>
        /// <param name="EndPoint">   ゴール地点   </param>
        /// <returns> 推定コスト </returns>
        protected byte GetHeuristicCost(Vector3Int StratPoint, Vector3Int EndPoint)
        {
            byte heuristic_cost;      // 推定コスト
            Vector3Int calculation;   // 計算用Vector



            calculation = new Vector3Int();

            // ヒューリスティックコスト計算
            calculation.x = Mathf.Abs(StratPoint.x - EndPoint.x);
            calculation.y = Mathf.Abs(StratPoint.y - EndPoint.y);
            calculation.z = Mathf.Abs(StratPoint.z - EndPoint.z);

            heuristic_cost = (byte)(calculation.x + calculation.y + calculation.z);

            return heuristic_cost;
        }


        /// <summary> 水中に入ってしまったなどの移動不可対策 </summary>
        /// <param name="LoopCount"> カウント </param>
        protected void LoopOut(byte LoopCount)
        {
            InitEntity = false;
            if (LoopCount >= AIData.LOOP_ERROR)
            {
                InitEntity = true;
            }
        }


        /// <summary> 次のマスの高さを取得する。 </summary>
        /// <param name="Direction"> 確認する方向 </param>
        /// <param name="Point"> 確認座標 </param>
        /// <returns> 次のマスの高さ </returns>
        public sbyte GetAdjacentCell(Direction8 Direction, Vector3Int Point, byte Prev = byte.MaxValue)
        {
            NaviData[,,] navi;   // マップ情報



            navi = DungeonManager.Map.NaviMap;

            // 隣接しているマスの高さ
            // 斜めの場合は一マス前後に移動させた後に移動できるまでy軸を動かす。
            switch (Direction)
            {
                case Direction8.FORWARD:
                    return navi[Point.x, Point.y, Point.z].Forward;

                case Direction8.RIGHT:
                    return navi[Point.x, Point.y, Point.z].Right;

                case Direction8.BACK:
                    return navi[Point.x, Point.y, Point.z].Back;

                case Direction8.LEFT:
                    return navi[Point.x, Point.y, Point.z].Left;


                case Direction8.RIGHT_FORWARD:

                    if (DiagonalCheck(Direction, Point) == false) return sbyte.MaxValue;


                    Point += Vector3Int.forward;

                    // 隣のマスが空欄以外なら無視する
                    if (navi[Point.x, Point.y, Point.z].Right > 0) return sbyte.MaxValue;

                    // 移動できるか確認
                    for (sbyte i = 0; i <= Point.y; ++i)
                    {
                        if (navi[Point.x, Point.y - i, Point.z].Right != sbyte.MaxValue) return (sbyte)(navi[Point.x, Point.y - i, Point.z].Right - i);
                    }

                    return navi[Point.x, Point.y, Point.z].Right;

                case Direction8.RIGHT_BACK:

                    if (DiagonalCheck(Direction, Point) == false) return sbyte.MaxValue;

                    Point += Vector3Int.back;

                    // 隣のマスが空欄以外なら無視する
                    if (navi[Point.x, Point.y, Point.z].Right > 0) return sbyte.MaxValue;

                    // 移動できるか確認
                    for (sbyte i = 0; i <= Point.y; ++i)
                    {
                        if (navi[Point.x, Point.y - i, Point.z].Right != sbyte.MaxValue) return (sbyte)(navi[Point.x, Point.y - i, Point.z].Right - i);
                    }

                    return navi[Point.x, Point.y, Point.z].Right;

                case Direction8.LEFT_BACK:

                    if (DiagonalCheck(Direction, Point) == false) return sbyte.MaxValue;

                    Point += Vector3Int.back;

                    // 隣のマスが空欄以外なら無視する
                    if (navi[Point.x, Point.y, Point.z].Left > 0) return sbyte.MaxValue;

                    // 移動できるか確認
                    for (sbyte i = 0; i <= Point.y; ++i)
                    {
                        if (navi[Point.x, Point.y - i, Point.z].Left != sbyte.MaxValue) return (sbyte)(navi[Point.x, Point.y - i, Point.z].Left - i);
                    }

                    return navi[Point.x, Point.y, Point.z].Left;

                case Direction8.LEFT_FORWARD:

                    if (DiagonalCheck(Direction, Point) == false) return sbyte.MaxValue;

                    Point += Vector3Int.forward;

                    // 隣のマスが空欄以外なら無視する
                    if (navi[Point.x, Point.y, Point.z].Left > 0) return sbyte.MaxValue;

                    // 移動できるか確認
                    for (sbyte i = 0; i <= Point.y; ++i)
                    {
                        if (navi[Point.x, Point.y - i, Point.z].Left != sbyte.MaxValue) return (sbyte)(navi[Point.x, Point.y - i, Point.z].Left - i);
                    }

                    return navi[Point.x, Point.y, Point.z].Left;


                default:
                    return sbyte.MaxValue;

            }
        }



        /// <summary> 斜め方向の確認 </summary>
        /// <param name="Direction">  移動方角 </param>
        /// <param name="CheckPoint"> 確認地点   </param>
        public bool DiagonalCheck(Direction8 Direction, Vector3Int CheckPoint)
        {
            ICube cube_type;   // キューブ情報



            // 斜め方向に移動する際の水平方向のチェック
            if (Direction.IsDiagonal() == true)
            {
                cube_type = DungeonManager.Map[CheckPoint.x + Direction.GetFrontPosition().x, CheckPoint.y, CheckPoint.z];
                if (cube_type.CubeType != CubeType.AIR) return false;

                cube_type = DungeonManager.Map[CheckPoint.x, CheckPoint.y, CheckPoint.z + Direction.GetFrontPosition().z];
                if (cube_type.CubeType != CubeType.AIR) return false;
            }

            return true;
        }



        /// <summary> 斜め方向の確認 </summary>
        /// <param name="StartPoint"> 移動方角 </param>
        /// <returns> true = スタック中
        /// <para> false = 通常動作 </para> </returns>
        public bool StackOut(Vector3Int StartPoint, Vector3Int EndPoint)
        {
            const byte LIMIT_COUNT = 6;   // 距離



            if (prev_point_ == EndPoint ||
                prev_point_2_ == EndPoint)
            {
                stack_count_++;
            }
            else
            {
                stack_count_ = 0;
            }

            DLog.Log(stack_count_);
            if (stack_count_ >= LIMIT_COUNT) return true;

            prev_point_2_ = prev_point_;
            prev_point_ = StartPoint;

            return false;
        }
    }
}