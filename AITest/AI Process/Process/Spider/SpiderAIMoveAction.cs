/**********************************************************************/
//  ファイル名 :
//      SpiderMovement
//  説明 :
//      Spider移動管理
/// 一部地形で進行不可
/**********************************************************************/

using Dungeon.Entities.Enemy;
using System.Collections.Generic;
using UnityEngine;


namespace Dungeon.Entities.AI
{
    public sealed class SpiderAIMoveAction : AIMoveAction
    {
        /// <summary> まっすぐ進む確率 </summary>
        private readonly byte forward_percent_;


        // チェーンメソッドの対策
        private EntityMovement Movement => entity_.Movement;
        private Vector3Int EnemyPosition => entity_.MapPosition;
        private byte JumpHeight => (byte)entity_.JumpHeight;



        private SpiderAIMoveAction() { }

        public SpiderAIMoveAction(EnemyEntity E, byte Forward)
        {
            // 値の初期化
            to_goal_ = new List<Vector3Int>();
            goal_index_ = 1;
            entity_ = E;
            forward_percent_ = Forward;

            Destination = Vector3Int.zero;
            DestinationFace = Direction8.FORWARD;
            MovementType = MoveType.HORIZONTAL;
            IsAreaChange = false;
        }


        /// <summary> ランダムな方向に移動 </summary>
        public void RandomMove()
        {
            Direction8 random_move;   // ランダム方向を格納する
            byte random_forward;      // 前方方向に進む確率
            bool can_move;            // 移動可能かチェック



            random_forward = (byte)Random.Range(AIData.PERCENT_RANDOM_MIN, AIData.PERCENT_RANDOM_MAX);
            random_move = (Direction8)Random.Range((int)Direction8.FORWARD, (int)Direction8.LEFT_FORWARD + 1);

            // CANMoveをCEAWLに切り替えるもの
            entity_.ChangeMovement(MoveMobility.CRAWL);

            // 移動可能範囲を探す
            for (byte i = 0; ; ++i)
            {
                random_move = (Direction8)Random.Range((int)Direction8.FORWARD, (int)Direction8.LEFT_FORWARD + 1);
                can_move = Movement.CanMove(random_move);

                LoopOut(i);

                if (can_move == true) break;     // 進行方向決定
                if (InitEntity == true) break;   // 座標初期化
            }


            // 確率かつ移動可能の場合に正面方向に移動
            if (random_forward <= forward_percent_ &&
                Movement.CanMove(Direction8.FORWARD) == true)
            {
                random_move = Direction8.FORWARD;
            }

            SpiderChangeMovement(random_move);

            DestinationFace = random_move;
        }


        /// <summary> Astar計算 </summary>
        /// <param name="StratPoint"> スタート地点 </param>
        /// <param name="EndPoint">   ゴール地点   </param>
        public void AstarCalculation(Vector3Int StratPoint, Vector3Int EndPoint)
        {
            Vector3Int node;            // 周囲座標
            Vector3Int trget_node;      // 最短座標
            ICube cube_type;            // 移動先のブロックチェック
            byte heuristic_cost;        // 推定コスト
            byte temp_heuristic_cost;   // 推定コスト退避先



            heuristic_cost = byte.MinValue;
            temp_heuristic_cost = byte.MaxValue;
            trget_node = Vector3Int.zero;

            // CANMoveをCEAWLに切り替えるもの
            entity_.ChangeMovement(MoveMobility.CRAWL);

            // 垂直に移動
            if (StratPoint.x == EndPoint.x &&
                StratPoint.z == EndPoint.z)
            {
                node = StratPoint;
                // 最短経路アルゴリズム
                for (Direction8 i = 0; i <= Direction8.LEFT_FORWARD; ++i)
                {
                    if (i.IsDiagonal() == false)
                    {
                        node += i.GetFrontPosition();
                        cube_type = DungeonManager.Map[node.x, node.y, node.z];

                        if (cube_type.CubeType != CubeType.AIR)
                        {
                            Destination = (node + Vector3Int.up);
                            DestinationFace = i;

                            // 高さを見て移動する。
                            if ((EndPoint.y - StratPoint.y) < 0)
                            {
                                Destination = (node + Vector3Int.down);
                                DestinationFace = i;
                                return;
                            }
                        }
                    }
                }
            }

            // 最短経路アルゴリズム
            for (Direction8 i = 0; i <= Direction8.LEFT_FORWARD; ++i)
            {
                trget_node = StratPoint;
                node = StratPoint;

                // 移動可能の場合はnodeを計算する。
                /// MEMO: ただし斜め移動の場合は現状無視(あとで作るか考える)
                if (i.IsDiagonal() == false)
                {
                    if (StratPoint.y == EndPoint.y &&
                        (StratPoint.x == EndPoint.x || StratPoint.z == EndPoint.z) &&
                        entity_.Ground != EvoDirection.DOWN)
                    {
                        if (i.GetFrontPosition(entity_.Ground) == Vector3Int.down)
                        {
                            node += Vector3Int.down;
                            cube_type = DungeonManager.Map[node.x, node.y, node.z];

                            if (cube_type.CubeType != CubeType.AIR)
                            {
                                Destination = node;
                                DestinationFace = i;
                            }
                        }


                    }

                    trget_node += i.GetFrontPosition(entity_.Ground);
                    cube_type = DungeonManager.Map[trget_node.x, trget_node.y, trget_node.z];

                    if (cube_type.CubeType == CubeType.AIR)
                    {
                        node = trget_node;
                    }

                    // 登録した座標を無視する
                    foreach (var j in to_goal_)
                    {
                        if (j == node)
                        {
                            break;
                        }
                    }

                    heuristic_cost = GetHeuristicCost(node, EndPoint);
                    // 最短経路を算出
                    if (temp_heuristic_cost > heuristic_cost)
                    {
                        temp_heuristic_cost = heuristic_cost;
                        Destination = node;
                        DestinationFace = i;
                    }
                }
            }

            SpiderChangeMovement(DestinationFace);
        }


        /// <summary> ランダムな方向に移動 </summary>
        public void AreaChange(Vector3Int StratPoint, Vector3Int EndPoint)
        {
            const byte NONE_NODE = 0;     // nodeがない場合
            const byte INDEX_STRAT = 1;   // 探索開始地点

            Vector3Int shortest_connected;   // 通路座標
            bool end_exploration;   // 探索終了条件



            // 条件継続
            IsAreaChange = false;
            shortest_connected = Vector3Int.zero;
            end_exploration = false;

            // 通路の最短経路を算出
            if (to_goal_.Count == NONE_NODE)
            {
                //shortest_connected = GetConnected(MapPosition);
                shortest_connected = StratPoint;
                to_goal_.Add(EndPoint);

                /// MEMO 
                /// ビームサーチを応用して探索、スタート地点とゴール地点から同時に探索して最短経路を探す。

                // ゴールを見つけるまで索敵
                while (end_exploration == false)
                {
                    AstarCalculation(to_goal_[to_goal_.Count - AIData.ASTAR_INDEX_OFFSET], shortest_connected);

                    to_goal_.Add(Destination);

                    // アンダーフロー対策
                    if (to_goal_[to_goal_.Count - AIData.ASTAR_INDEX_OFFSET] == shortest_connected)
                    {
                        end_exploration = true;
                    }
                }
            }

            Destination = to_goal_[goal_index_];

            // 次の座標を参照
            if (goal_index_ < to_goal_.Count - AIData.ASTAR_INDEX_OFFSET)
            {
                goal_index_++;
            }
            // Areachange 処理終わり
            else
            {
                to_goal_.Clear();

                goal_index_ = INDEX_STRAT;
                IsAreaChange = true;
            }
        }



        /// <summary> 最短距離の通路座標を取得 </summary>
        /// <param name="StratPoint"> スタート地点 </param>
        /// <returns> 通路座標 </returns>
        private Vector3Int GetConnected(Vector3Int StratPoint)
        {
            List<Generator.Room> Point;     // 通路
            ICube cube_type;                // マップキューブ情報
            Vector3Int position_A;          // PointA座標
            Vector3Int position_B;          // PointB座標
            Vector3Int shortest_position;   // 通路座標

            byte heuristic_cost_A;        // PointA推定コスト
            byte heuristic_cost_B;        // PointB推定コスト
            byte temp_heuristic_cost_A;   // PointA推定コスト退避先
            byte temp_heuristic_cost_B;   // PointA推定コスト退避先
            bool can_search;              // 探索可能の場合



            Point = DungeonManager.Map.Rooms;
            position_A = new Vector3Int();
            position_B = new Vector3Int();
            shortest_position = new Vector3Int();

            heuristic_cost_A = byte.MinValue;
            heuristic_cost_B = byte.MinValue;
            temp_heuristic_cost_A = byte.MaxValue;
            temp_heuristic_cost_B = byte.MaxValue;
            can_search = false;

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
                shortest_position = position_A;
            }
            else
            {
                shortest_position = position_B;
            }

            cube_type = cube_type = DungeonManager.Map[shortest_position.x, shortest_position.y, shortest_position.z];

            // A*探索が可能になるまでY座標を編集する。
            while (can_search == false)
            {
                if (cube_type.CubeType != CubeType.AIR) break;
                shortest_position.y--;
                cube_type = cube_type = DungeonManager.Map[shortest_position.x, shortest_position.y, shortest_position.z];

            }

            return shortest_position;
        }


        /// <summary> 足元方向の座標を取得 </summary>
        /// <param name="Face"> 張り付いている方向 </param>
        private Vector3Int FootVector(EvoDirection Face)
        {
            // 各EvoDirectionの足元方向の座標を取得
            switch (Face)
            {
                case EvoDirection.UP:
                    return Vector3Int.up;

                case EvoDirection.DOWN:
                    return Vector3Int.down;

                case EvoDirection.RIGHT:
                    return Vector3Int.right;

                case EvoDirection.LEFT:
                    return Vector3Int.left;

                case EvoDirection.FORWARD:
                    return Vector3Int.forward;

                case EvoDirection.BACK:
                    return Vector3Int.back;

                default:
                    return Vector3Int.zero;
            }
        }


        /// <summary> Movementの切り替え </summary>
        private void SpiderChangeMovement(Direction8 ObjDirection)
        {
            entity_.ChangeMovement(MoveMobility.CRAWL);
            if (GetAdjacentCell(ObjDirection, entity_.MapPosition) <= 1 &&
                GetAdjacentCell(ObjDirection, entity_.MapPosition) >= -1 &&
                entity_.Ground == EvoDirection.DOWN)
            {
                entity_.ChangeMovement(MoveMobility.GROUND);
            }
        }
    }
}