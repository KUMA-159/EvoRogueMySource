/**********************************************************************/
//  説明 :
//      Type.Flyの処理
/**********************************************************************/

using UnityEngine;

/// MEMO: Area,Romingがおかしい


namespace Dungeon.Entities.AI
{
    public sealed class FlywayAIProcess : AIStetaCall
    {
        /// <summary> 移動処理 </summary>
        private FlywayAIMoveAction move_action_;
        /// <summary> まっすぐ進むカウンタ </summary>
        private byte forward_count_;
        /// <summary> まっすぐ進む上限 </summary>
        private byte forward_limit_;



        // 短縮
        private EntityMovement Movement => enemy_entity_.Movement;
        private Vector3Int EnemyPosition => enemy_entity_.MapPosition;
        private Vector3Int PlayerPosition => player_entity_.MapPosition;



        private FlywayAIProcess() { }

        public FlywayAIProcess(AIEntity Entitys)
        {
            init_position_ = Entitys.enemy_.MapPosition;
            enemy_entity_ = Entitys.enemy_;
            player_entity_ = Entitys.player_;

            EndAreaChange = false;
            IsRoaming = false;

            move_action_ = new FlywayAIMoveAction(enemy_entity_);
            forward_count_ = 0;
            forward_limit_ = (byte)Random.Range(1, 6);
        }



        /// <summary> 
        /// Wait処理を呼ぶ
        /// <para> 処理なし </para>
        /// </summary>
        protected internal override void CallNoop() { }


        /// <summary> Roaming処理を呼ぶ </summary>
        protected internal override void CallRoaming()
        {
            // 方向はカウント数が一定の際に変更する
            if (forward_count_ >= forward_limit_)
            {
                // 上限値は毎回変更される
                forward_limit_ = (byte)Random.Range(3, 8);
                forward_count_ = 0;

                // 進行方向を決定
                move_action_.RandomMove();
            }

            // 壁にぶつかった場合は再度向きを変える。
            if (move_action_.MoveCheck(EnemyPosition,
                move_action_.DestinationFace,
                move_action_.MovementType) == false)
            {
                forward_count_ = 0;

                // 進行方向を決定
                move_action_.RandomMove();
            }

            // 座標初期化
            if (move_action_.InitEntity == true)
            {
                SetInitEntity();
            }

            forward_count_++;

            enemy_entity_.ChangeEntityDirection(move_action_.DestinationFace);
            Movement.MoveTo(Direction8.FORWARD, move_action_.MovementType);
        }


        /// <summary> Tracking処理を呼ぶ </summary>
        protected internal override void CallTracking()
        {
            //IS_DEBUG
            //if (DebugDistance(EnemyPosition, PlayerPosition) == false) return;



            // A*処理
            move_action_.AstarCalculation(EnemyPosition, PlayerPosition);

            // AIの状態を変化させる
            IsRoaming = move_action_.StackOut(EnemyPosition, move_action_.Destination);

            // プレイヤーの方向を向く
            enemy_entity_.LookToPosition(move_action_.Destination);
            Movement.MoveTo(Direction8.FORWARD, move_action_.MovementType);
        }


        /// <summary> AreaChange処理を呼ぶ </summary>
        protected internal override void CallAreaChange()
        {
            //IS_DEBUG
            //if (DebugDistance(EnemyPosition, PlayerPosition) == false) return;



            // エリアチェンジ
            move_action_.AreaChange();
            EndAreaChange = move_action_.IsAreaChange;

            enemy_entity_.LookToPosition(move_action_.Destination);
            Movement.MoveTo(Direction8.FORWARD, move_action_.MovementType);

        }


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