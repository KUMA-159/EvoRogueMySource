/**********************************************************************/
//  説明 :
//      Single移動管理
/// 斜め移動の場合にバグが起こりやすい
/**********************************************************************/

using Dungeon.Entities.Enemy;
using System.Collections.Generic;
using UnityEngine;


namespace Dungeon.Entities.AI
{
    public sealed class SingleAIMoveAction : AIMoveAction
    {
        /// <summary> 前回の座標 </summary>
        private Vector3Int prev_position_;
        /// <summary> まっすぐ進む確率 </summary>
        private readonly byte forward_percent_;



        // チェーンメソッドの対策
        private EntityMovement Movement => entity_.Movement;
        private Vector3Int EnemyPosition => entity_.MapPosition;
        private byte JumpHeight => (byte)entity_.JumpHeight;



        private SingleAIMoveAction() { }

        /// <param name="E"> EnemyEntity情報 </param>
        /// <param name="Forward"> 前進する確率 </param>
        public SingleAIMoveAction(EnemyEntity E, byte Forward)
        {
            // 値の初期化
            entity_ = E;
            to_goal_ = new List<Vector3Int>();

            prev_position_ = Vector3Int.zero;
            goal_index_ = 0;
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

            // 移動可能範囲を探す
            for (byte i = 0; ; ++i)
            {
                random_move = (Direction8)Random.Range((int)Direction8.FORWARD, (int)Direction8.LEFT_FORWARD + 1);
                can_move = (GetAdjacentCell(random_move, EnemyPosition) <= JumpHeight);

                LoopOut(i);

                if (InitEntity == true || can_move == true) break;   // 座標初期化 | 進行方向決定
            }

            can_move = (GetAdjacentCell(Direction8.FORWARD, EnemyPosition) <= JumpHeight);

            // 確率かつ移動可能の場合に正面方向に移動
            if (random_forward <= forward_percent_ &&
                can_move == true)
            {
                random_move = Direction8.FORWARD;
            }

            DestinationFace = random_move;
        }


        /// <summary> Astar計算 </summary>
        /// <param name="StratPoint"> スタート地点 </param>
        /// <param name="EndPoint">   ゴール地点   </param>
        public void AstarCalculation(Vector3Int StratPoint, Vector3Int EndPoint)
        {
            Vector3Int node;            // 周囲座標
            byte heuristic_cost;        // 推定コスト
            byte temp_heuristic_cost;   // 推定コスト退避先
            bool is_move;               // 移動可能か判定をとる



            temp_heuristic_cost = byte.MaxValue;

            // 最短経路アルゴリズム
            for (Direction8 i = 0; i <= Direction8.LEFT_FORWARD; ++i)
            {
                node = StratPoint;
                is_move = Movement.CanMove(i);

                // 移動可能ならそのマス座標を格納
                if (is_move == true)
                {
                    node += i.GetFrontPosition();
                    node.y += GetAdjacentCell(i, StratPoint);

                    is_move = (node != prev_position_);
                }

                // 登録した座標を無視する
                foreach (var j in to_goal_)
                {
                    if (j == node)
                    {
                        is_move = false;
                        break;
                    }
                }

                // コスト計算
                if (is_move == true)
                {
                    // ヒューリスティックコスト計算
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

            prev_position_ = StratPoint;
        }


        /// <summary> ランダムな方向に移動 </summary>
        public void AreaChange(Vector3Int EndPoint, byte LoopOut = byte.MaxValue)
        {
            const byte NONE_NODE = 0;

            Vector3Int connect_point;   // 通路座標
            bool is_search_end;



            // 初期化
            connect_point = Vector3Int.zero;
            is_search_end = false;
            IsAreaChange = false;

            connect_point = EndPoint;
            // 255だった場合は通路を探索する。
            if (LoopOut == byte.MaxValue)
            {
                connect_point = GetConnected(EndPoint);
            }

            // 座標と同じ場合はエリアチェンジを行わない。
            if (connect_point == entity_.MapPosition) return;

            // 通路の最短経路を算出
            if (to_goal_.Count == NONE_NODE)
            {
                to_goal_.Add(connect_point);

                // LoopOutまで索敵
                for (byte i = 0; LoopOut > i; ++i)
                {
                    to_goal_.Add(ReverseAstarCalculation(to_goal_[to_goal_.Count - 1], EnemyPosition));

                    // 探索地点がかぶったら探索終了
                    if (EnemyPosition == to_goal_[to_goal_.Count - 1])
                    {
                        goal_index_ = (byte)(to_goal_.Count - 2);
                        is_search_end = true;
                        break;
                    }
                }

                if (is_search_end == false)
                {
                    goal_index_ = (byte)(to_goal_.Count - 2);
                }
            }

            Destination = to_goal_[goal_index_];

            // 次の座標を参照
            if (goal_index_ != 0)
            {
                goal_index_--;
            }
            // Areachange 処理終わり
            else
            {
                NodeClear();
                IsAreaChange = true;
            }
        }


        /// <summary> ノードを初期化 </summary>
        public void NodeClear()
        {
            to_goal_.Clear();
            goal_index_ = 0;
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
            Vector3Int connect_point;   // 通路座標

            byte heuristic_cost_A;        // PointA推定コスト
            byte heuristic_cost_B;        // PointB推定コスト
            byte temp_heuristic_cost_A;   // PointA推定コスト退避先
            byte temp_heuristic_cost_B;   // PointA推定コスト退避先



            Point = DungeonManager.Map.Rooms;
            position_A = new Vector3Int();
            position_B = new Vector3Int();
            connect_point = new Vector3Int();

            heuristic_cost_A = byte.MinValue;
            heuristic_cost_B = byte.MinValue;
            temp_heuristic_cost_A = byte.MaxValue;
            temp_heuristic_cost_B = byte.MaxValue;

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
                connect_point = position_A;
            }
            else
            {
                connect_point = position_B;
            }

            // A*探索が可能になるまでY座標を編集する。
            while (true)
            {
                cube_type = DungeonManager.Map[connect_point.x, connect_point.y - 1, connect_point.z];

                if (cube_type.CubeType != CubeType.AIR) break;

                connect_point.y--;
            }

            DLog.Log(connect_point);
            return connect_point;
        }


        /// <summary> Astar計算 </summary>
        /// <param name="StratPoint"> スタート地点 </param>
        /// <param name="EndPoint">   ゴール地点   </param>
        private Vector3Int ReverseAstarCalculation(Vector3Int StratPoint, Vector3Int EndPoint)
        {
            sbyte cell_height;           // 次のマスの高さ
            byte heuristic_cost;         // 推定コスト
            byte temp_heuristic_cost;    // 推定コスト退避先
            bool is_move;                // 移動可能か判定をとる
            Vector3Int node;             // 周囲座標
            Vector3Int trget_position;   // 最短座標



            temp_heuristic_cost = byte.MaxValue;
            trget_position = Vector3Int.zero;

            // 最短経路アルゴリズム
            for (Direction8 i = 0; i <= Direction8.LEFT_FORWARD; ++i)
            {
                node = StratPoint;
                cell_height = GetAdjacentCell(i, StratPoint);
                is_move = (cell_height <= JumpHeight && cell_height >= -JumpHeight);

                // to_start_の移動可能チェック
                if (is_move == true)
                {
                    node += i.GetFrontPosition();
                    node.y += cell_height;

                    is_move = (node != prev_position_);
                }

                // 登録した座標を無視する
                foreach (var j in to_goal_)
                {
                    if (j == node)
                    {
                        is_move = false;
                        break;
                    }
                }

                // コスト計算
                if (is_move == true)
                {
                    // ヒューリスティックコスト計算
                    heuristic_cost = GetHeuristicCost(node, EndPoint);

                    // 最短経路を算出
                    if (temp_heuristic_cost > heuristic_cost)
                    {
                        temp_heuristic_cost = heuristic_cost;
                        trget_position = node;
                    }
                }
            }

            return trget_position;
        }
    }
}