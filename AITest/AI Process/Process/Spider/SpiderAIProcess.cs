/**********************************************************************/
//  説明 :
//      Type.Spiderの処理
/// MEMO: 一部地形でのバグあり
/// MEMO: 斜め移動で移動ができない
/// MEMO: プレイヤーが真上にいる場合に追跡できない。
/**********************************************************************/

using UnityEngine;


namespace Dungeon.Entities.AI
{
    public sealed class SpiderAIProcess : AIStetaCall
    {
        /// <summary> 移動処理 </summary>
        private SpiderAIMoveAction move_action_;

        // 短縮
        private EntityMovement Movement => enemy_entity_.Movement;
        private Vector3Int Eposition => enemy_entity_.MapPosition;
        private Vector3Int Pposition => player_entity_.MapPosition;



        public SpiderAIProcess(AIEntity Entitys, byte ForwardPercent)
        {
            // エンティティ初期化
            init_position_ = Entitys.enemy_.MapPosition;
            enemy_entity_ = Entitys.enemy_;
            player_entity_ = Entitys.player_;

            // 移動処理
            move_action_ = new SpiderAIMoveAction(enemy_entity_, ForwardPercent);
        }


        /// <summary> Roaming処理を呼ぶ </summary>
        protected internal override void CallRoaming()
        {
            //IS_DEBUG
            if (DebugDistance(Eposition, Pposition) == false) return;



            // 移動先を決める
            move_action_.RandomMove();

            // 移動要求を出す
            Movement.MoveTo(move_action_.DestinationFace);

            // 座標初期化
            if (move_action_.InitEntity == true)
            {
                SetInitEntity();
            }
        }


        /// <summary> Tracking処理を呼ぶ </summary>
        protected internal override void CallTracking()
        {
            //IS_DEBUG
            if (DebugDistance(Eposition, Pposition) == false) return;



            // A*処理
            move_action_.AstarCalculation(Eposition, Pposition);

            // プレイヤーの方向を向く
            enemy_entity_.ChangeEntityDirection(move_action_.DestinationFace);
            enemy_entity_.Movement.MoveTo(Direction8.FORWARD);
        }


        /// <summary> AreaChange処理を呼ぶ </summary>
        protected internal override void CallAreaChange()
        {
            IsRoaming = true;
            ////#if UNITY_EDITOR
            ////            byte heuristic_cost;      // 推定コスト
            ////            Vector3Int calculation;   // 計算用Vector



            ////            DLog.Log(enemy_entity_.Ground, 36);
            ////            calculation = new Vector3Int();

            ////            calculation.x = Mathf.Abs(enemy_entity_.MapPosition.x - player_entity_.MapPosition.x);
            ////            calculation.y = Mathf.Abs(enemy_entity_.MapPosition.y - player_entity_.MapPosition.y);
            ////            calculation.z = Mathf.Abs(enemy_entity_.MapPosition.z - player_entity_.MapPosition.z);

            ////            heuristic_cost = (byte)(calculation.x + calculation.y + calculation.z);
            ////            if (heuristic_cost <= 30)
            ////            {
            ////                // エリアチェンジ
            ////                //move_action_.AreaChange(enemy_entity_, Eposition);
            ////                move_action_.AreaChange(Pposition, Eposition);
            ////                EndAreaChange = move_action_.IsAreaChange;

            ////                enemy_entity_.LookToPosition(move_action_.Destination);
            ////                enemy_entity_.Movement.MoveTo(Direction8.FORWARD);
            ////            }
            ////#else
            ////            // エリアチェンジ
            ////            //move_action_.AreaChange(enemy_entity_, Eposition);
            ////            move_action_.AreaChange(enemy_entity_, player_entity_, Eposition);
            ////            EndAreaChange = move_action_.IsAreaChange;

            ////            enemy_entity_.LookToPosition(move_action_.Destination);
            ////            enemy_entity_.Movement.MoveTo(Direction8.FORWARD);
            ////#endif
        }



        private SpiderAIProcess() { }

        /// <summary> 
        /// Noop処理を呼ぶ
        /// <para> 処理なし </para>
        /// </summary>
        protected internal override void CallNoop() { }

        /// <summary> 
        /// Sleep処理を呼ぶ
        /// <para> 処理なし </para>
        /// </summary>
        protected internal override void CallSleep() { }

        /// <summary> 
        /// Escape処理を呼ぶ
        /// <para> 処理なし </para>
        /// </summary>
        protected internal override void CallEscape() { }
    }
}