/**********************************************************************/
//  説明 :
//      Flyway移動管理
/**********************************************************************/

// 上、下、左、右などすべてに対応した移動開発
// 移動可能
// Movecheckの確認

using Dungeon.Entities.Enemy;
using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Entities.AI
{
    public sealed class FlywayAIMoveAction : AIMoveAction
    {
        /// <summary> y軸カウンタ </summary>
        private byte y_count_;
        /// <summary> 上下移動数 </summary>
        private byte y_move_count_;
        /// <summary> ゴールからスタートの最短経路 </summary>
        private List<MoveType> astar_movement;


        // チェーンメソッドの対策
        private EntityMovement Movement => entity_.Movement;



        public FlywayAIMoveAction(EnemyEntity E)
        {
            entity_ = E;

            to_goal_ = new List<Vector3Int>();
            MovementType = MoveType.HORIZONTAL;
            goal_index_ = 1;
            y_move_count_ = 0;
            y_count_ = 0;
            astar_movement = new List<MoveType>();

            Destination = Vector3Int.zero;
            DestinationFace = Direction8.FORWARD;
            MovementType = MoveType.HORIZONTAL;
            IsAreaChange = false;
        }


        /// <summary> Astar計算 </summary>
        /// <param name="MapPosition"> 対象の座標 </param>
        /// <param name="Direction">   進行方向   </param>
        public bool MoveCheck(Vector3Int MapPosition, Direction8 Direction, MoveType Type)
        {
            ICube cube_type;            // キューブ情報
            Vector3Int side_position;   // 水平方向の座標
            bool is_block;              // 水平方向が移動可能か確認



            cube_type = DungeonManager.Map[MapPosition.x, MapPosition.y, MapPosition.z];
            side_position = MapPosition;
            is_block = true;

            // 移動可能か確認
            switch (Type)
            {
                case MoveType.ONLY_UP:

                    MapPosition += Vector3Int.up;
                    cube_type = DungeonManager.Map[MapPosition.x, MapPosition.y, MapPosition.z];
                    break;

                case MoveType.ONLY_DOWN:

                    MapPosition += Vector3Int.down;
                    cube_type = DungeonManager.Map[MapPosition.x, MapPosition.y, MapPosition.z];
                    break;

                case MoveType.UP:

                    // 斜め方向に移動する際の水平方向のチェック
                    if (DiagonalCheck(Direction, side_position) == false) return false;

                    // 水平の移動方向のチェック
                    side_position += Direction.GetFrontPosition();
                    cube_type = DungeonManager.Map[side_position.x, side_position.y, side_position.z];
                    if (cube_type.CubeType != CubeType.AIR) return false;

                    side_position = MapPosition;
                    side_position += Vector3Int.up;

                    // 垂直方向のチェック
                    cube_type = DungeonManager.Map[side_position.x, side_position.y, side_position.z];
                    if (cube_type.CubeType != CubeType.AIR) return false;

                    // 斜め方向に移動する際の水平 + y1 方向のチェック
                    if (DiagonalCheck(Direction, side_position) == false) return false;


                    // 移動方向のチェック
                    MapPosition += Vector3Int.up;
                    MapPosition += Direction.GetFrontPosition();
                    cube_type = DungeonManager.Map[MapPosition.x, MapPosition.y, MapPosition.z];
                    break;

                case MoveType.DOWN:

                    // 斜め方向に移動する際の水平方向のチェック
                    if (DiagonalCheck(Direction, side_position) == false) return false;

                    // 水平の移動方向のチェック
                    side_position += Direction.GetFrontPosition();
                    cube_type = DungeonManager.Map[side_position.x, side_position.y, side_position.z];
                    if (cube_type.CubeType != CubeType.AIR) return false;

                    side_position = MapPosition;
                    side_position += Vector3Int.down;

                    // 垂直方向のチェック
                    cube_type = DungeonManager.Map[side_position.x, side_position.y, side_position.z];
                    if (cube_type.CubeType != CubeType.AIR) return false;

                    // 斜め方向に移動する際の水平 + y1 方向のチェック
                    if (DiagonalCheck(Direction, side_position) == false) return false;


                    // 移動方向のチェック
                    MapPosition += Vector3Int.down;
                    MapPosition += Direction.GetFrontPosition();
                    cube_type = DungeonManager.Map[MapPosition.x, MapPosition.y, MapPosition.z];
                    break;

                default:
                    // 斜め方向に移動する際の水平方向のチェック
                    if (DiagonalCheck(Direction, MapPosition) == false) return false;

                    MapPosition += Direction.GetFrontPosition();
                    cube_type = DungeonManager.Map[MapPosition.x, MapPosition.y, MapPosition.z];
                    break;
            }

            if (cube_type.CubeType == CubeType.AIR && is_block == true) return true;

            return false;
        }


        /// <summary> ランダムな方向に移動 </summary>
        public void RandomMove()
        {
            const byte TEBLE_MIN = 0;     // テーブルの最小値
            const byte TEBLE_MAX = 7;     // テーブルの最大値
            const byte Y_COUNT_MAX = 4;   // テーブルの最大値

            ICube cube_type;             // マップキューブ情報
            Direction8 random_move;      // 移動の種類テーブル
            Vector3Int check_position;   // 確認用の座標


            random_move = new Direction8();
            y_count_++;

            // 移動可能のマスを検索
            for (byte i = 0; ; ++i)
            {
                LoopOut(i);
                random_move = (Direction8)Random.Range(TEBLE_MIN, TEBLE_MAX);
                check_position = entity_.MapPosition;

                // 座標初期化
                if (InitEntity == true) break;

                // 一定条件かでしかY軸移動をしない
                if (y_count_ < Y_COUNT_MAX)
                {
                    MovementType = MoveType.HORIZONTAL;
                    y_move_count_ = 0;
                }
                // y軸の移動数を決める
                else
                {
                    while (MovementType == MoveType.HORIZONTAL)
                    {
                        MovementType = RandomMoveType();
                    }

                    if (y_move_count_ == 0)
                    {
                        y_move_count_ = (byte)Random.Range(2, 5);
                    }
                    else
                    {
                        y_move_count_--;

                        if (y_move_count_ == 0)
                        {
                            y_count_ = 0;
                        }
                    }
                }

                // 移動可能かチェック
                switch (MovementType)
                {
                    // 一つ上の座標を確認
                    case MoveType.UP:
                        check_position += Vector3Int.up;

                        if (MoveCheck(check_position, random_move, MovementType) == true)
                        {
                            DestinationFace = random_move;
                            return;
                        }

                        break;

                    // 一つ下の座標を確認
                    case MoveType.DOWN:
                        check_position += Vector3Int.down;

                        if (MoveCheck(check_position, random_move, MovementType) == true)
                        {
                            DestinationFace = random_move;
                            return;
                        }

                        break;

                    // 一つ下の座標を確認
                    case MoveType.ONLY_UP:
                        check_position += Vector3Int.up;
                        cube_type = DungeonManager.Map[check_position.x, check_position.y, check_position.z];

                        if (cube_type.CubeType == CubeType.AIR)
                        {
                            DestinationFace = random_move;
                            return;
                        }

                        break;

                    // 一つ下の座標を確認
                    case MoveType.ONLY_DOWN:
                        check_position += Vector3Int.down;
                        cube_type = DungeonManager.Map[check_position.x, check_position.y, check_position.z];

                        if (cube_type.CubeType == CubeType.AIR)
                        {
                            DestinationFace = random_move;
                            return;
                        }

                        break;

                    default:
                        if (MoveCheck(check_position, random_move, MovementType) == true)
                        {
                            DestinationFace = random_move;
                            return;
                        }

                        break;
                }
            }
        }


        /// <summary> Astar計算 </summary>
        /// <param name="StratPoint"> スタート地点 </param>
        /// <param name="EndPoint">   ゴール地点   </param>
        public void AstarCalculation(Vector3Int StratPoint, Vector3Int EndPoint)
        {
            Vector3Int temp_destination;   // 仮の目的地
            byte heuristic_cost;           // 推定コスト
            byte temp_heuristic_cost;      // 推定コスト退避先
            MoveType type;                 // 移動type
            ICube cube_type;               // キューブ情報


            type = EstimationMoveType(StratPoint, EndPoint);

            // HORIZONTALチェック
            MovementType = MoveType.HORIZONTAL;
            ShortestPath(StratPoint, EndPoint);
            temp_destination = Destination;

            // EstimationMoveTypeチェック
            //if (type != MoveType.HORIZONTAL)
            //{
            MovementType = type;
            ShortestPath(StratPoint, EndPoint);
            //}

            // HORIZONTALとEstimationMoveTypeのヒューリスティックコスト計算
            heuristic_cost = GetHeuristicCost(Destination, EndPoint);
            temp_heuristic_cost = GetHeuristicCost(temp_destination, EndPoint);

            // 最短経路を算出
            if (temp_heuristic_cost <= heuristic_cost)
            {
                MovementType = MoveType.HORIZONTAL;
                temp_heuristic_cost = heuristic_cost;
                Destination = temp_destination;
            }


            heuristic_cost = GetHeuristicCost(StratPoint + Vector3Int.up, EndPoint);
            cube_type = DungeonManager.Map[StratPoint.x, StratPoint.y + 1, StratPoint.z];

            // 最短経路を算出
            if (cube_type.CubeType == CubeType.AIR &&
                temp_heuristic_cost > heuristic_cost)
            {
                MovementType = MoveType.ONLY_UP;
                temp_heuristic_cost = heuristic_cost;
                Destination = StratPoint + Vector3Int.up;
            }

            heuristic_cost = GetHeuristicCost(StratPoint + Vector3Int.down, EndPoint);
            cube_type = DungeonManager.Map[StratPoint.x, StratPoint.y - 1, StratPoint.z];

            // 最短経路を算出
            if (cube_type.CubeType == CubeType.AIR &&
                temp_heuristic_cost > heuristic_cost)
            {
                MovementType = MoveType.ONLY_DOWN;
                Destination = StratPoint + Vector3Int.down;
            }
        }


        /// <summary> ランダムな方向に移動 </summary>
        public void AreaChange()
        {
            const byte NONE_NODE = 0;     // nodeがない場合
            const byte INDEX_STRAT = 1;   // 探索開始地点

            Vector3Int shortest_connected;   // 通路座標



            shortest_connected = new Vector3Int();
            // 条件継続
            IsAreaChange = false;



            // 通路の最短経路を算出
            if (to_goal_.Count == NONE_NODE)
            {
                shortest_connected = GetConnected(entity_.MapPosition);

                // 座標と同じ場合はエリアチェンジを行わない。
                if (shortest_connected == entity_.MapPosition) return;

                to_goal_.Add(entity_.MapPosition);
                astar_movement.Add(MoveType.HORIZONTAL);


                // ゴールを見つけるまで索敵
                for (byte i = 0; ; ++i)
                {
                    AstarCalculation(to_goal_[to_goal_.Count - AIData.ASTAR_INDEX_OFFSET], shortest_connected);
                    to_goal_.Add(Destination);

                    //EstimationMoveType(to_goal_[to_goal_.Count - 2], to_goal_[to_goal_.Count - AIData.ASTAR_INDEX_OFFSET]);
                    astar_movement.Add(MovementType);

                    // 探索地点がかぶったら探索終了
                    if (to_goal_[to_goal_.Count - AIData.ASTAR_INDEX_OFFSET] == shortest_connected) break;

                    // 探索強制終了
                    if (i >= 99)
                    {
                        DLog.Error("探索強制終了");
                        break;
                    }
                }
            }

            // 座標更新
            Destination = to_goal_[goal_index_];
            MovementType = astar_movement[goal_index_];

            // 次の座標を参照
            if (goal_index_ < to_goal_.Count - AIData.ASTAR_INDEX_OFFSET)
            {
                goal_index_++;
            }
            // Areachange 処理終わり
            else
            {
                to_goal_.Clear();
                astar_movement.Clear();

                DLog.Log("Clear");
                goal_index_ = INDEX_STRAT;
                IsAreaChange = true;
            }
        }



        /// <summary> 推定方向でMoveTypeを設定する。 </summary>
        /// <param name="StratPoint"> スタート地点 </param>
        /// <param name="EndPoint">   ゴール地点   </param>
        private MoveType EstimationMoveType(Vector3Int Strat, Vector3Int Goal)
        {
            byte range_differencial;   // プレイヤーとエネミーの距離差分



            // 差分を計算
            range_differencial = (byte)((Strat.x - Goal.x) + (Strat.z - Goal.z));


            if (Strat.y > Goal.y) return MoveType.DOWN;      // プレイヤーが下にいる場合
            else if (Strat.y < Goal.y) return MoveType.UP;   // プレイヤーが上にいる場合

            return MoveType.HORIZONTAL;
        }


        /// <summary> MoveTypeを設定する。 </summary>
        /// <returns> MoveType </returns>
        private MoveType RandomMoveType()
        {
            const byte TEBLE_MIN = 0;   // テーブルの最小値
            const byte TEBLE_MAX = 5;   // テーブルの最大値

            MoveType[] movetype_dt = new MoveType[5]   // 移動の種類テーブル
            {
                MoveType.HORIZONTAL,
                MoveType.UP,
                MoveType.DOWN,
                MoveType.ONLY_UP,
                MoveType.ONLY_DOWN
            };



            // テーブルの値から移動種類をランダムに決める
            return movetype_dt[Random.Range(TEBLE_MIN, TEBLE_MAX)];
        }


        /// <summary> 最短距離の通路座標を取得 </summary>
        /// <param name="StratPoint"> スタート地点 </param>
        /// <returns> 通路座標 </returns>
        private Vector3Int GetConnected(Vector3Int StratPoint)
        {
            List<Generator.Room> Point;     // 通路
            byte heuristic_cost_A;          // PointA推定コスト
            byte heuristic_cost_B;          // PointB推定コスト
            byte temp_heuristic_cost_A;     // PointA推定コスト退避先
            byte temp_heuristic_cost_B;     // PointA推定コスト退避先
            Vector3Int position_A;          // PointA座標
            Vector3Int position_B;          // PointB座標
            Vector3Int shortest_position;   // 通路座標



            Point = DungeonManager.Map.Rooms;
            heuristic_cost_A = byte.MinValue;
            heuristic_cost_B = byte.MinValue;
            temp_heuristic_cost_A = byte.MaxValue;
            temp_heuristic_cost_B = byte.MaxValue;
            position_A = new Vector3Int();
            position_B = new Vector3Int();
            shortest_position = new Vector3Int();

            // 最短距離の通路を取得
            foreach (var i in Point)
            {
                foreach (var j in i.Connected)
                {
                    heuristic_cost_A = GetHeuristicCost(StratPoint, j.PointA);
                    heuristic_cost_B = GetHeuristicCost(StratPoint, j.PointB);


                    // point A、B 最短座標を取得
                    if (temp_heuristic_cost_A > heuristic_cost_A)
                    {
                        temp_heuristic_cost_A = heuristic_cost_A;
                        position_A = j.PointA;
                    }
                    if (temp_heuristic_cost_B > heuristic_cost_B)
                    {
                        temp_heuristic_cost_B = heuristic_cost_B;
                        position_B = j.PointB;
                    }
                }
            }

            // 最短座標を決める
            if (heuristic_cost_B > heuristic_cost_A)
            {
                shortest_position = position_B;
            }
            else
            {
                shortest_position = position_A;
            }

            DLog.Log(shortest_position);
            return shortest_position;
        }


        /// <summary> 最短距離処理 </summary>
        /// <param name="StratPoint"> スタート地点 </param>
        /// <param name="EndPoint">   ゴール地点   </param>
        private void ShortestPath(Vector3Int StratPoint, Vector3Int EndPoint)
        {
            byte heuristic_cost;        // 推定コスト
            byte temp_heuristic_cost;   // 推定コスト退避先
            bool is_move;               // 移動可能か判定をとる
            Vector3Int node;            // 周囲座標



            heuristic_cost = byte.MinValue;
            temp_heuristic_cost = byte.MaxValue;
            node = Vector3Int.zero;

            // 最短経路アルゴリズム
            for (Direction8 i = 0; i <= Direction8.LEFT_FORWARD; ++i)
            {
                node = StratPoint;

                // 移動可能か確認
                is_move = MoveCheck(node, i, MovementType);

                // MoveTypeの方向にY軸をずらす。
                if (is_move == true &&
                    MovementType == MoveType.UP)
                {
                    node += Vector3Int.up;
                }
                else if (is_move == true &&
                    MovementType == MoveType.DOWN)
                {
                    node += Vector3Int.down;
                }

                // 方向が移動可能か確認
                if (is_move == true)
                {
                    node += i.GetFrontPosition();
                }

                foreach (var j in to_goal_)
                {
                    if (j == node)
                    {
                        is_move = false;
                        break;
                    }
                }

                if (is_move == true)
                {
                    // ヒューリスティックコスト計算
                    heuristic_cost = GetHeuristicCost(node, EndPoint);

                    // 最短経路を算出
                    if (temp_heuristic_cost > heuristic_cost)
                    {
                        temp_heuristic_cost = heuristic_cost;
                        Destination = node;
                    }
                }
            }

        }


        ///// <summary> 斜め方向の確認 </summary>
        ///// <param name="Direction">  移動方角 </param>
        ///// <param name="CheckPoint"> 確認地点   </param>
        //private bool DiagonalCheck(Direction8 Direction, Vector3Int CheckPoint)
        //{
        //    ICube cube_type;   // キューブ情報


        //    // 斜め方向に移動する際の水平方向のチェック
        //    if (Direction.IsDiagonal() == true)
        //    {
        //        cube_type = DungeonManager.Map[CheckPoint.x + Direction.GetFrontPosition().x, CheckPoint.y, CheckPoint.z];
        //        if (cube_type.CubeType != CubeType.AIR) return false;

        //        cube_type = DungeonManager.Map[CheckPoint.x, CheckPoint.y, CheckPoint.z + Direction.GetFrontPosition().z];
        //        if (cube_type.CubeType != CubeType.AIR) return false;
        //    }

        //    return true;
        //}
    }
}